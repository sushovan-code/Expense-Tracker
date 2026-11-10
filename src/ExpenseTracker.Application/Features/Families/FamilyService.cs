using ExpenseTracker.Application.Abstractions.Families;
using ExpenseTracker.Application.Abstractions.Identity;
using ExpenseTracker.Application.Abstractions.Persistence;
using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Security;
using ExpenseTracker.Domain.Categories;
using ExpenseTracker.Domain.Common;
using ExpenseTracker.Domain.Families;
using Microsoft.Extensions.Logging;

namespace ExpenseTracker.Application.Features.Families;

public interface IFamilyService
{
    Task<Result<FamilyDto>> GetMineAsync(CancellationToken cancellationToken);
    Task<Result<FamilyDto>> CreateAsync(string name, CancellationToken cancellationToken);
    Task<Result<FamilyDto>> RenameAsync(string name, CancellationToken cancellationToken);
    Task<Result<FamilyDto>> AddMemberAsync(AddMemberCommand command, CancellationToken cancellationToken);
    Task<Result<CreatedInviteDto>> CreateInviteAsync(FamilyRole role, CancellationToken cancellationToken);
    Task<Result> RevokeInviteAsync(Guid inviteId, CancellationToken cancellationToken);

    /// <summary>Anonymous: creates the account and joins the family.</summary>
    Task<Result> AcceptInviteAsync(AcceptInviteCommand command, CancellationToken cancellationToken);

    /// <summary>A signed-in user without a family joins with an invite token.</summary>
    Task<Result<FamilyDto>> JoinAsync(string token, CancellationToken cancellationToken);

    Task<Result> ChangeRoleAsync(Guid userId, FamilyRole role, CancellationToken cancellationToken);
    Task<Result> RemoveMemberAsync(Guid userId, CancellationToken cancellationToken);
    Task<Result> LeaveAsync(CancellationToken cancellationToken);
}

internal sealed partial class FamilyService(
    ICurrentFamily current,
    IFamilyRepository families,
    IFamilyQueries queries,
    ICategoryRepository categories,
    IUserAccountStore accounts,
    IUnitOfWork unitOfWork,
    TimeProvider clock,
    ILogger<FamilyService> logger) : IFamilyService
{
    public async Task<Result<FamilyDto>> GetMineAsync(CancellationToken cancellationToken)
    {
        if (current.FamilyId is not { } familyId) return FamilyErrors.NoFamily;
        return await LoadAsync(familyId, cancellationToken);
    }

    public async Task<Result<FamilyDto>> CreateAsync(string name, CancellationToken cancellationToken)
    {
        if (current.FamilyId is not null) return FamilyErrors.AlreadyInFamily;

        var created = Family.Create(name, clock.GetUtcNow());
        if (!created.IsSuccess) return created.Error;
        var family = created.Value;

        families.Add(family);
        families.Add(FamilyMember.Create(family.Id, current.UserId, FamilyRole.Owner, clock.GetUtcNow()));
        foreach (var (categoryName, color) in DefaultCategories.All)
        {
            categories.Add(Category.Create(categoryName, color, null, family.Id).Value);
        }
        await unitOfWork.SaveChangesAsync(cancellationToken);

        LogFamilyChanged(logger, family.Id, "created");
        return await LoadAsync(family.Id, cancellationToken);
    }

    public async Task<Result<FamilyDto>> RenameAsync(string name, CancellationToken cancellationToken)
    {
        if (RequireManager() is { } denied) return denied;
        var family = await families.GetFamilyAsync(current.FamilyId!.Value, cancellationToken);
        if (family is null) return FamilyErrors.NoFamily;

        var renamed = family.Rename(name);
        if (!renamed.IsSuccess) return renamed.Error;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        LogFamilyChanged(logger, family.Id, "renamed");
        return await LoadAsync(family.Id, cancellationToken);
    }

    public async Task<Result<FamilyDto>> AddMemberAsync(AddMemberCommand command, CancellationToken cancellationToken)
    {
        if (RequireManager() is { } denied) return denied;
        var familyId = current.FamilyId!.Value;

        var added = await CreateAccountAsync(command.Email, command.DisplayName, command.Password, cancellationToken);
        if (!added.IsSuccess) return added.Error;

        families.Add(FamilyMember.Create(familyId, added.Value, command.Role, clock.GetUtcNow()));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        LogFamilyChanged(logger, familyId, "member added");
        return await LoadAsync(familyId, cancellationToken);
    }

    public async Task<Result<CreatedInviteDto>> CreateInviteAsync(FamilyRole role, CancellationToken cancellationToken)
    {
        if (RequireManager() is { } denied) return denied;

        var token = InviteTokens.NewToken();
        var invite = FamilyInvite.Create(current.FamilyId!.Value, role, InviteTokens.Hash(token), current.UserId, clock.GetUtcNow());
        families.Add(invite);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        LogFamilyChanged(logger, invite.FamilyId, "invite created");
        return new CreatedInviteDto(invite.Id, token, invite.Role, invite.ExpiresAt);
    }

    public async Task<Result> RevokeInviteAsync(Guid inviteId, CancellationToken cancellationToken)
    {
        if (RequireManager() is { } denied) return denied;

        var invite = await families.GetInviteAsync(current.FamilyId!.Value, inviteId, cancellationToken);
        if (invite is null) return FamilyErrors.InviteNotFound(inviteId);

        invite.Revoke(clock.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);

        LogFamilyChanged(logger, invite.FamilyId, "invite revoked");
        return Result.Success();
    }

    public async Task<Result> AcceptInviteAsync(AcceptInviteCommand command, CancellationToken cancellationToken)
    {
        var invite = await FindUsableInviteAsync(command.Token, cancellationToken);
        if (invite is null) return FamilyErrors.InvalidInvite;

        var added = await CreateAccountAsync(command.Email, command.DisplayName, command.Password, cancellationToken);
        if (!added.IsSuccess) return added.Error;

        return await JoinFamilyAsync(invite, added.Value, cancellationToken);
    }

    public async Task<Result<FamilyDto>> JoinAsync(string token, CancellationToken cancellationToken)
    {
        if (current.FamilyId is not null) return FamilyErrors.AlreadyInFamily;

        var invite = await FindUsableInviteAsync(token, cancellationToken);
        if (invite is null) return FamilyErrors.InvalidInvite;

        var joined = await JoinFamilyAsync(invite, current.UserId, cancellationToken);
        return joined.IsSuccess ? await LoadAsync(invite.FamilyId, cancellationToken) : joined.Error;
    }

    public async Task<Result> ChangeRoleAsync(Guid userId, FamilyRole role, CancellationToken cancellationToken)
    {
        if (RequireManager() is { } denied) return denied;
        var familyId = current.FamilyId!.Value;

        var member = await families.GetMemberAsync(familyId, userId, cancellationToken);
        if (member is null) return FamilyErrors.MemberNotFound(userId);
        if (await WouldLeaveNoOwnerAsync(member, role, cancellationToken)) return FamilyErrors.LastOwner;

        member.ChangeRole(role);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        LogFamilyChanged(logger, familyId, "role changed");
        return Result.Success();
    }

    public async Task<Result> RemoveMemberAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (current.FamilyId is not { } familyId) return FamilyErrors.NoFamily;
        if (userId != current.UserId && RequireManager() is { } denied) return denied;

        var member = await families.GetMemberAsync(familyId, userId, cancellationToken);
        if (member is null) return FamilyErrors.MemberNotFound(userId);
        if (await WouldLeaveNoOwnerAsync(member, newRole: null, cancellationToken)) return FamilyErrors.LastOwner;

        families.Remove(member);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        LogFamilyChanged(logger, familyId, "member removed");
        return Result.Success();
    }

    public Task<Result> LeaveAsync(CancellationToken cancellationToken) => RemoveMemberAsync(current.UserId, cancellationToken);

    private Error? RequireManager()
    {
        if (current.FamilyId is null || current.Role is not { } role) return FamilyErrors.NoFamily;
        return FamilyPolicy.CanManageFamily(role) ? null : FamilyErrors.NotAllowed;
    }

    private async Task<bool> WouldLeaveNoOwnerAsync(FamilyMember member, FamilyRole? newRole, CancellationToken cancellationToken) =>
        FamilyPolicy.WouldLeaveNoOwner(member.Role, newRole, await families.CountOwnersAsync(member.FamilyId, cancellationToken));

    private async Task<FamilyInvite?> FindUsableInviteAsync(string token, CancellationToken cancellationToken)
    {
        var invite = await families.FindInviteByHashAsync(InviteTokens.Hash(token ?? ""), cancellationToken);
        return invite is not null && invite.IsUsable(clock.GetUtcNow()) ? invite : null;
    }

    private async Task<Result> JoinFamilyAsync(FamilyInvite invite, Guid userId, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var accepted = invite.Accept(now);
        if (!accepted.IsSuccess) return accepted.Error;

        families.Add(FamilyMember.Create(invite.FamilyId, userId, invite.Role, now));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        LogFamilyChanged(logger, invite.FamilyId, "invite accepted");
        return Result.Success();
    }

    // New accounts get the default app role; family actions never change app roles.
    private Task<Result<Guid>> CreateAccountAsync(string email, string displayName, string password, CancellationToken cancellationToken) =>
        accounts.CreateAsync(new NewUserAccount(email.Trim(), displayName.Trim(), clock.GetUtcNow()), password, [AppRoles.User], cancellationToken);

    private async Task<Result<FamilyDto>> LoadAsync(Guid familyId, CancellationToken cancellationToken)
    {
        var family = await queries.GetAsync(familyId, clock.GetUtcNow(), cancellationToken);
        if (family is null) return FamilyErrors.NoFamily;

        // Read the caller's role from the members list: a family created in this request isn't in the context yet.
        var role = family.Members.FirstOrDefault(m => m.UserId == current.UserId)?.Role ?? FamilyRole.Viewer;
        return family with
        {
            MyRole = role,
            Invites = FamilyPolicy.CanManageFamily(role) ? family.Invites : [],
        };
    }

    [LoggerMessage(EventId = LogEvents.FamilyChanged, Level = LogLevel.Information, Message = "Family {FamilyId}: {Change}")]
    private static partial void LogFamilyChanged(ILogger logger, Guid familyId, string change);
}

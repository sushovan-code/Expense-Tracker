using ExpenseTracker.Domain.Families;

namespace ExpenseTracker.Application.Features.Families;

/// <summary>One of my families and my role in it.</summary>
public sealed record FamilySummaryDto(Guid Id, string Name, FamilyRole Role);

public sealed record FamilyMemberDto(Guid UserId, string DisplayName, string Email, FamilyRole Role, DateTimeOffset JoinedAt);

public sealed record InviteDto(Guid Id, FamilyRole Role, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt);

/// <param name="MyRole">The caller's role, so the UI knows what to show.</param>
/// <param name="Invites">Open invites; only filled in for owners.</param>
public sealed record FamilyDto(
    Guid Id, string Name, FamilyRole MyRole, IReadOnlyList<FamilyMemberDto> Members, IReadOnlyList<InviteDto> Invites);

/// <summary>The invite link's secret is returned only here, once; only its hash is stored.</summary>
public sealed record CreatedInviteDto(Guid Id, string Token, FamilyRole Role, DateTimeOffset ExpiresAt);

public sealed record AddMemberCommand(string Email, string DisplayName, string Password, FamilyRole Role);

public sealed record AcceptInviteCommand(string Token, string Email, string DisplayName, string Password);

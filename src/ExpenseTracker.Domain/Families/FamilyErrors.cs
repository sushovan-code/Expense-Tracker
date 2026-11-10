using ExpenseTracker.Domain.Common;

namespace ExpenseTracker.Domain.Families;

public static class FamilyErrors
{
    public static readonly Error NoFamily =
        Error.Forbidden("Create or join a family first.");

    public static readonly Error NotAllowed =
        Error.Forbidden("Your family role doesn't allow this.");

    public static readonly Error AlreadyInFamily =
        Error.Conflict("Already in this family", "This person already belongs to this family.");

    public static readonly Error AlreadyMemberElsewhere =
        Error.Conflict("Already a member of another family",
            "A person can be a Member or Viewer in only one family. They can still own any number of families.");

    public static readonly Error LastOwner =
        Error.Conflict("Last owner", "A family needs at least one owner.");

    public static readonly Error InvalidInvite =
        Error.Conflict("Invite not valid", "This invite link is expired, revoked or already used.");

    public static Error MemberNotFound(Guid userId) => Error.NotFound("Family member", userId);

    public static Error InviteNotFound(Guid id) => Error.NotFound("Invite", id);
}

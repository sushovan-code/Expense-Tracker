namespace ExpenseTracker.Domain.Common;

public enum ErrorKind
{
    Validation,
    NotFound,
    Conflict,
    /// <summary>The caller could not be authenticated (bad credentials, expired or revoked session).</summary>
    Unauthorized,
    /// <summary>The caller is authenticated but not allowed to do this.</summary>
    Forbidden,
}

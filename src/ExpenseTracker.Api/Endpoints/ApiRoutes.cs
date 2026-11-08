namespace ExpenseTracker.Api.Endpoints;

/// <summary>
/// The versioned prefix for every REST endpoint. A breaking contract change gets a new version
/// (<c>/api/v2</c>) mapped alongside this one. The SignalR hub and health checks are not versioned.
/// </summary>
internal static class ApiRoutes
{
    public const string Prefix = "/api/v1";

    /// <summary>The authentication endpoints; also the refresh cookie's path, so the cookie is sent nowhere else.</summary>
    public const string Auth = Prefix + "/auth";
}

using System.Diagnostics;
using ExpenseTracker.Api.Realtime;
using ExpenseTracker.Application.Common;

namespace ExpenseTracker.Api.Middleware;

/// <summary>
/// One structured "request completed" log per API call: method, route template (low cardinality,
/// unlike raw paths), status code and duration. Also opens a logging scope with the caller's client
/// id so every log written while handling the request can be tied back to the browser tab.
/// </summary>
internal sealed partial class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await next(context);
            return;
        }

        var clientId = context.Request.Headers[SignalRChangeNotifier.ClientIdHeader].ToString();
        using var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["ClientId"] = string.IsNullOrEmpty(clientId) ? null : clientId,
        });

        var started = Stopwatch.GetTimestamp();
        try
        {
            await next(context);
        }
        finally
        {
            var elapsedMs = Math.Round(Stopwatch.GetElapsedTime(started).TotalMilliseconds, 1);
            var route = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? context.Request.Path.Value ?? "";
            var status = context.Response.StatusCode;
            var level = status >= 500 ? LogLevel.Error : status >= 400 ? LogLevel.Warning : LogLevel.Information;

            LogRequestCompleted(logger, level, context.Request.Method, route, status, elapsedMs);
        }
    }

    [LoggerMessage(EventId = LogEvents.RequestCompleted,
        Message = "HTTP {HttpMethod} {HttpRoute} responded {StatusCode} in {ElapsedMs} ms")]
    private static partial void LogRequestCompleted(
        ILogger logger, LogLevel level, string httpMethod, string httpRoute, int statusCode, double elapsedMs);
}

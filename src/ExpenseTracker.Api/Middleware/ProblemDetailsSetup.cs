using ExpenseTracker.Application.Common;

namespace ExpenseTracker.Api.Middleware;

/// <summary>Problem details customisation: trace id on every error, and one log line per validation failure.</summary>
internal static partial class ProblemDetailsSetup
{
    public static void Customize(ProblemDetailsContext ctx)
    {
        ctx.ProblemDetails.Extensions.TryAdd("traceId", ctx.HttpContext.TraceIdentifier);

        // One place that logs every 400 validation failure (framework or domain), with field names.
        if (ctx.ProblemDetails is HttpValidationProblemDetails { Errors.Count: > 0 } validation)
        {
            var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                .CreateLogger("ExpenseTracker.Api.Validation");
            var route = (ctx.HttpContext.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText
                ?? ctx.HttpContext.Request.Path.Value ?? "";
            LogValidationFailed(logger, ctx.HttpContext.Request.Method, route,
                string.Join(", ", validation.Errors.Keys),
                string.Join(" | ", validation.Errors.SelectMany(e => e.Value)));
        }
    }

    [LoggerMessage(EventId = LogEvents.RequestValidationFailed, Level = LogLevel.Warning,
        Message = "Validation failed for {HttpMethod} {HttpRoute}: fields {InvalidFields} — {ValidationErrors}")]
    private static partial void LogValidationFailed(
        ILogger logger, string httpMethod, string httpRoute, string invalidFields, string validationErrors);
}

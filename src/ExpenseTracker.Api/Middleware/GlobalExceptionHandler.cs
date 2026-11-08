using ExpenseTracker.Application.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Api.Middleware;

/// <summary>
/// Last-resort handler: logs unexpected exceptions and returns a problem details response
/// without leaking internals to the client.
/// </summary>
internal sealed partial class GlobalExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
        {
            // The client went away; nothing useful to send back.
            return true;
        }

        var route = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? context.Request.Path.Value ?? "";
        LogUnhandledException(logger, exception, context.Request.Method, route, exception.GetType().Name);

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Something went wrong",
                Detail = "An unexpected error occurred. Please try again.",
            },
        });
    }

    [LoggerMessage(EventId = LogEvents.UnhandledException, Level = LogLevel.Error,
        Message = "Unhandled {ExceptionType} while handling {HttpMethod} {HttpRoute}")]
    private static partial void LogUnhandledException(
        ILogger logger, Exception exception, string httpMethod, string httpRoute, string exceptionType);
}

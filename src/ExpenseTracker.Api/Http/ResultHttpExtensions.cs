using ExpenseTracker.Domain.Common;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Api.Http;

/// <summary>Maps <see cref="Result"/> values onto HTTP responses (RFC 9457 problem details on failure).</summary>
public static class ResultHttpExtensions
{
    public static Results<TSuccess, ProblemHttpResult> ToHttpResult<T, TSuccess>(
        this Result<T> result, Func<T, TSuccess> onSuccess)
        where TSuccess : IResult
    {
        if (result.IsSuccess) return onSuccess(result.Value);
        return result.Error.ToProblem();
    }

    public static Results<NoContent, ProblemHttpResult> ToHttpResult(this Result result)
    {
        if (result.IsSuccess) return TypedResults.NoContent();
        return result.Error.ToProblem();
    }

    public static ProblemHttpResult ToProblem(this Error error)
    {
        var status = error.Kind switch
        {
            ErrorKind.Validation => StatusCodes.Status400BadRequest,
            ErrorKind.NotFound => StatusCodes.Status404NotFound,
            ErrorKind.Conflict => StatusCodes.Status409Conflict,
            ErrorKind.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorKind.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError,
        };

        // Field errors use the same shape as framework validation failures,
        // so clients only have one error format to handle.
        ProblemDetails problem = error.Field is null
            ? new ProblemDetails { Title = error.Title, Detail = error.Detail, Status = status }
            : new HttpValidationProblemDetails(new Dictionary<string, string[]> { [error.Field] = [error.Detail] })
            {
                Title = error.Title,
                Status = status,
            };

        return TypedResults.Problem(problem);
    }
}

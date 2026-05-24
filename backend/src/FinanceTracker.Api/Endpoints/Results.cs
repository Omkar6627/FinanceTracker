using FinanceTracker.Application.Common;
using Microsoft.AspNetCore.Http;

namespace FinanceTracker.Api.Endpoints;

public static class ResultExtensions
{
    public record ErrorBody(string Message, IReadOnlyList<string>? Errors = null);

    public static IResult ToHttpResult(this Result r)
    {
        return r.Status switch
        {
            ResultStatus.Ok => Results.NoContent(),
            ResultStatus.NotFound => Results.NotFound(new ErrorBody(r.Error ?? "Not found")),
            ResultStatus.Conflict => Results.Conflict(new ErrorBody(r.Error ?? "Conflict")),
            ResultStatus.Forbidden => Results.Forbid(),
            ResultStatus.Unauthorized => Results.Unauthorized(),
            ResultStatus.Validation => Results.BadRequest(new ErrorBody(r.Error ?? "Validation failed", r.ValidationErrors)),
            _ => Results.Problem(r.Error ?? "Unexpected error")
        };
    }

    public static IResult ToHttpResult<T>(this Result<T> r, int successStatus = StatusCodes.Status200OK)
    {
        if (r.IsSuccess)
            return successStatus == StatusCodes.Status201Created
                ? Results.Json(r.Value, statusCode: StatusCodes.Status201Created)
                : Results.Ok(r.Value);

        return ((Result)r).ToHttpResult();
    }
}

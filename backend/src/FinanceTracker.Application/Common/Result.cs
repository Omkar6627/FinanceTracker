namespace FinanceTracker.Application.Common;

public enum ResultStatus
{
    Ok = 0,
    NotFound = 1,
    Conflict = 2,
    Forbidden = 3,
    Unauthorized = 4,
    Validation = 5,
    Error = 6
}

public class Result
{
    public ResultStatus Status { get; }
    public string? Error { get; }
    public IReadOnlyList<string> ValidationErrors { get; }

    protected Result(ResultStatus status, string? error = null, IReadOnlyList<string>? validation = null)
    {
        Status = status;
        Error = error;
        ValidationErrors = validation ?? Array.Empty<string>();
    }

    public bool IsSuccess => Status == ResultStatus.Ok;

    public static Result Success() => new(ResultStatus.Ok);
    public static Result NotFound(string msg = "Resource not found") => new(ResultStatus.NotFound, msg);
    public static Result Conflict(string msg) => new(ResultStatus.Conflict, msg);
    public static Result Forbidden(string msg = "Access denied") => new(ResultStatus.Forbidden, msg);
    public static Result Unauthorized(string msg = "Not authenticated") => new(ResultStatus.Unauthorized, msg);
    public static Result Validation(IReadOnlyList<string> errs) => new(ResultStatus.Validation, "Validation failed", errs);
    public static Result Validation(string err) => new(ResultStatus.Validation, "Validation failed", new[] { err });
    public static Result Failure(string msg) => new(ResultStatus.Error, msg);
}

public class Result<T> : Result
{
    public T? Value { get; }

    private Result(T value) : base(ResultStatus.Ok) { Value = value; }
    private Result(ResultStatus status, string? error, IReadOnlyList<string>? validation = null)
        : base(status, error, validation) { Value = default; }

    public static Result<T> Success(T value) => new(value);
    public new static Result<T> NotFound(string msg = "Resource not found") => new(ResultStatus.NotFound, msg);
    public new static Result<T> Conflict(string msg) => new(ResultStatus.Conflict, msg);
    public new static Result<T> Forbidden(string msg = "Access denied") => new(ResultStatus.Forbidden, msg);
    public new static Result<T> Unauthorized(string msg = "Not authenticated") => new(ResultStatus.Unauthorized, msg);
    public new static Result<T> Validation(IReadOnlyList<string> errs) => new(ResultStatus.Validation, "Validation failed", errs);
    public new static Result<T> Validation(string err) => new(ResultStatus.Validation, "Validation failed", new[] { err });
    public new static Result<T> Failure(string msg) => new(ResultStatus.Error, msg);
}

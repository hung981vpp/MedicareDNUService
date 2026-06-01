namespace MedicalAPI.Application.Common;

public sealed class Result<T>
{
    public bool IsSuccess { get; init; }
    public int StatusCode { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }
    public IReadOnlyList<ApiError> Errors { get; init; } = [];

    public static Result<T> Ok(T data, string message = "Thành công", int statusCode = StatusCodes.Status200OK) => new()
    {
        IsSuccess = true,
        StatusCode = statusCode,
        Message = message,
        Data = data
    };

    public static Result<T> Fail(string message, int statusCode, params ApiError[] errors) => new()
    {
        IsSuccess = false,
        StatusCode = statusCode,
        Message = message,
        Errors = errors
    };
}

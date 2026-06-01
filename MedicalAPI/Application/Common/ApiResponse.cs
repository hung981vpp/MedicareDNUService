namespace MedicalAPI.Application.Common;

public sealed class ApiResponse<T>
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }
    public IReadOnlyList<ApiError> Errors { get; init; } = [];
    public string TraceId { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public static ApiResponse<T> Ok(T data, string message, string traceId) => new()
    {
        Success = true,
        Message = message,
        Data = data,
        TraceId = traceId
    };

    public static ApiResponse<T> Fail(string message, string traceId, params ApiError[] errors) => new()
    {
        Success = false,
        Message = message,
        TraceId = traceId,
        Errors = errors
    };
}

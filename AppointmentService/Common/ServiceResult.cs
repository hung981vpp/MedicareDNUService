namespace AppointmentService.Common;

public sealed class ServiceResult<T>
{
    private ServiceResult(bool success, string message, T? data, ServiceErrorType errorType)
    {
        Success = success;
        Message = message;
        Data = data;
        ErrorType = errorType;
    }

    public bool Success { get; }

    public string Message { get; }

    public T? Data { get; }

    public ServiceErrorType ErrorType { get; }

    public static ServiceResult<T> Ok(T data, string message)
    {
        return new ServiceResult<T>(true, message, data, ServiceErrorType.None);
    }

    public static ServiceResult<T> Fail(string message, ServiceErrorType errorType)
    {
        return new ServiceResult<T>(false, message, default, errorType);
    }
}

public enum ServiceErrorType
{
    None,
    NotFound,
    BadRequest
}

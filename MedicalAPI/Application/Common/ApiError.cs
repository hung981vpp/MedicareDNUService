namespace MedicalAPI.Application.Common;

public sealed record ApiError(string Field, string Code, string Message);

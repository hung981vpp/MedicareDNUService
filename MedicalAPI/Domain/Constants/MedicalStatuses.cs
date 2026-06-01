namespace MedicalAPI.Domain.Constants;

public static class MedicalStatuses
{
    public const string Active = "Đang hoạt động";
    public const string WaitingForExam = "Chờ khám";
    public const string InProgress = "Đang khám";
    public const string Completed = "Đã hoàn tất";
    public const string Cancelled = "Đã hủy";
    public const string Draft = "Bản nháp";
    public const string Locked = "Đã khóa";
    public const string Created = "Đã tạo";
    public const string SentToPharmacy = "Đã gửi nhà thuốc";
    public const string Dispensed = "Đã cấp phát";
    public const string Ordered = "Đã chỉ định";
    public const string Processed = "Đã xử lý";
    public const string PendingPublish = "Đang chờ gửi";
    public const string Published = "Đã gửi";
}

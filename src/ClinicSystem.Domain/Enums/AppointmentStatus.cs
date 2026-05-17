namespace ClinicSystem.Domain.Enums;

public enum AppointmentStatus
{
    Pending = 1,    // 待就诊
    InProgress = 2, // 就诊中
    Completed = 3,  // 已完成
    Cancelled = 4,  // 已取消
    NoShow = 5      // 爽约
}

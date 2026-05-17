using ClinicSystem.Domain.Enums;

namespace ClinicSystem.Domain.Entities;

/// <summary>
/// 挂号/预约
/// </summary>
public class Appointment
{
    public int Id { get; set; }
    public string AppointmentNo { get; set; } = string.Empty;   // 挂号编号
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public int DoctorId { get; set; }
    public User Doctor { get; set; } = null!;
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public int? RegisterUserId { get; set; }                     // 挂号操作人（护士/收银）
    public User? RegisterUser { get; set; }
    public DateTime AppointmentDate { get; set; }                // 就诊日期
    public string TimeSlot { get; set; } = string.Empty;         // 时段: 上午/下午/晚班
    public int QueueNumber { get; set; }                         // 排队序号
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
    public string? Source { get; set; }                          // 来源: 现场/微信预约
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
}

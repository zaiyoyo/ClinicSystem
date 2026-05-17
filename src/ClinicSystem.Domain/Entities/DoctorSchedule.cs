using ClinicSystem.Domain.Enums;

namespace ClinicSystem.Domain.Entities;

/// <summary>
/// 医生排班
/// </summary>
public class DoctorSchedule
{
    public int Id { get; set; }
    public int DoctorId { get; set; }
    public User Doctor { get; set; } = null!;
    public DayOfWeek DayOfWeek { get; set; }                     // 星期几
    public string TimeSlot { get; set; } = string.Empty;         // 时段: 上午/下午
    public TimeSpan StartTime { get; set; }                      // 开始时间
    public TimeSpan EndTime { get; set; }                        // 结束时间
    public int MaxPatients { get; set; } = 30;                   // 该时段限额
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 特殊排班（用于调休、节假日等覆盖常规排班）
/// </summary>
public class SpecialSchedule
{
    public int Id { get; set; }
    public int DoctorId { get; set; }
    public User Doctor { get; set; } = null!;
    public DateTime Date { get; set; }                           // 具体日期
    public bool IsWorkDay { get; set; }                          // true=上班, false=休息
    public string? TimeSlot { get; set; }                        // 上班时段（可选）
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public int? MaxPatients { get; set; }
    public string? Reason { get; set; }                          // 调休/节假日原因
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

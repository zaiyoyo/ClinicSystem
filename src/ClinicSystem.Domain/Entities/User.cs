using ClinicSystem.Domain.Enums;

namespace ClinicSystem.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;      // 登录账号
    public string PasswordHash { get; set; } = string.Empty;   // 密码哈希
    public string DisplayName { get; set; } = string.Empty;    // 显示姓名
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public UserRole Role { get; set; }                         // 角色
    public bool IsActive { get; set; } = true;                 // 是否启用
    public int? DepartmentId { get; set; }                     // 所属科室
    public Department? Department { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    // 医生专属字段
    public string? Title { get; set; }                         // 职称
    public string? Introduction { get; set; }                  // 医生简介
    public decimal? ConsultationFee { get; set; }              // 挂号费
    public int? MaxDailyPatients { get; set; }                 // 每日最大接诊量

    // Navigation
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
}

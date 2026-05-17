namespace ClinicSystem.Domain.Entities;

/// <summary>
/// 患者
/// </summary>
public class Patient
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Gender { get; set; }          // "男" / "女"
    public DateTime? DateOfBirth { get; set; }
    public string? Phone { get; set; }
    public string? IdCard { get; set; }          // 身份证号
    public string? Address { get; set; }
    public string? MedicalInsuranceNo { get; set; } // 医保号
    public string? Allergies { get; set; }       // 过敏史
    public string? MedicalHistory { get; set; }  // 既往病史
    public string? Remark { get; set; }
    public bool IsDeleted { get; set; }          // 软删除
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
}

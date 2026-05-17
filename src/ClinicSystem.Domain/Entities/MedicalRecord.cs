namespace ClinicSystem.Domain.Entities;

/// <summary>
/// 电子病历
/// </summary>
public class MedicalRecord
{
    public int Id { get; set; }
    public string RecordNo { get; set; } = string.Empty;         // 病历编号
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public int DoctorId { get; set; }
    public User Doctor { get; set; } = null!;
    public int AppointmentId { get; set; }
    public Appointment Appointment { get; set; } = null!;
    public DateTime VisitDate { get; set; }

    // 主诉
    public string? ChiefComplaint { get; set; }
    // 现病史
    public string? PresentIllness { get; set; }
    // 既往史
    public string? PastHistory { get; set; }
    // 体格检查
    public string? PhysicalExamination { get; set; }
    // 辅助检查
    public string? AuxiliaryExamination { get; set; }
    // 中医望闻问切
    public string? TcmObservation { get; set; }                  // 望诊
    public string? TcmAuscultation { get; set; }                 // 闻诊
    public string? TcmInquiry { get; set; }                      // 问诊
    public string? TcmPalpation { get; set; }                    // 切诊
    // 诊断
    public string? WesternDiagnosis { get; set; }                // 西医诊断
    public string? TcmDiagnosis { get; set; }                    // 中医诊断
    public string? TcmSyndrome { get; set; }                     // 中医证候
    // 医嘱
    public string? DoctorAdvice { get; set; }
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
}

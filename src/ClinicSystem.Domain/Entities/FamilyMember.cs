namespace ClinicSystem.Domain.Entities;

/// <summary>
/// 患者家庭成员
/// </summary>
public class FamilyMember
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;     // 关系: 配偶/父亲/母亲/子女/其他
    public string? Phone { get; set; }
    public string? IdCard { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

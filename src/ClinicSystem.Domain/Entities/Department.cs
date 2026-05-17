namespace ClinicSystem.Domain.Entities;

/// <summary>
/// 科室
/// </summary>
public class Department
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }           // 排序
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<User> Doctors { get; set; } = new List<User>();
}

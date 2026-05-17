namespace ClinicSystem.Domain.Entities;

/// <summary>
/// 处方头
/// </summary>
public class Prescription
{
    public int Id { get; set; }
    public string PrescriptionNo { get; set; } = string.Empty;
    public int MedicalRecordId { get; set; }
    public MedicalRecord MedicalRecord { get; set; } = null!;
    public string Type { get; set; } = "西药";                   // 西药/中成药/中药饮片/中药颗粒
    public string? DecoctingMethod { get; set; }                 // 煎药方法（中药）
    public string? Direction { get; set; }                       // 用法说明
    public decimal TotalAmount { get; set; }                     // 总金额
    public int? PharmacistId { get; set; }                       // 审核药师
    public User? Pharmacist { get; set; }
    public string Status { get; set; } = "待发药";               // 待发药/已发药/已退药
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DispensedAt { get; set; }                   // 发药时间

    // Navigation
    public ICollection<PrescriptionItem> Items { get; set; } = new List<PrescriptionItem>();
}

/// <summary>
/// 处方明细
/// </summary>
public class PrescriptionItem
{
    public int Id { get; set; }
    public int PrescriptionId { get; set; }
    public Prescription Prescription { get; set; } = null!;
    public int DrugId { get; set; }
    public Drug Drug { get; set; } = null!;
    public string DrugName { get; set; } = string.Empty;         // 冗余，药品名称
    public string? Specification { get; set; }                   // 规格
    public decimal Quantity { get; set; }                        // 数量
    public string? Dosage { get; set; }                          // 每次用量
    public string? Frequency { get; set; }                       // 频次 (如: tid)
    public string? Usage { get; set; }                           // 用法 (如: 口服)
    public string? Days { get; set; }                            // 天数
    public string? Remark { get; set; }                          // 备注
    public decimal UnitPrice { get; set; }                       // 单价
    public decimal SubTotal { get; set; }                        // 小计
}

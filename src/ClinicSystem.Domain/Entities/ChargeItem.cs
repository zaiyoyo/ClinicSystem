namespace ClinicSystem.Domain.Entities;

/// <summary>
/// 收费项目字典（挂号费、诊疗费、检查费、治疗费等）
/// </summary>
public class ChargeItem
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;             // 项目编码
    public string Name { get; set; } = string.Empty;             // 项目名称
    public string Category { get; set; } = string.Empty;         // 分类: 挂号/诊疗/检查/治疗/其他
    public decimal Price { get; set; }                           // 单价
    public string? Unit { get; set; }                            // 单位
    public bool IsActive { get; set; } = true;
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 药品字典
/// </summary>
public class Drug
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;             // 药品编码
    public string Name { get; set; } = string.Empty;             // 药品名称
    public string? CommonName { get; set; }                      // 通用名
    public string? Specification { get; set; }                   // 规格
    public string? Manufacturer { get; set; }                    // 生产厂家
    public string Category { get; set; } = "西药";               // 分类: 西药/中成药/中药饮片/中药颗粒/材料
    public string? DosageForm { get; set; }                      // 剂型
    public string? Unit { get; set; }                            // 单位
    public decimal Price { get; set; }                           // 零售价
    public decimal? CostPrice { get; set; }                      // 成本价
    public bool IsPrescription { get; set; } = true;             // 是否处方药
    public bool IsActive { get; set; } = true;
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

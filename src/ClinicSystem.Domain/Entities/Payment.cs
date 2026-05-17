namespace ClinicSystem.Domain.Entities;

/// <summary>
/// 收费记录
/// </summary>
public class Payment
{
    public int Id { get; set; }
    public string PaymentNo { get; set; } = string.Empty;        // 收费编号
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public int? AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }
    public int OperatorId { get; set; }                          // 操作人
    public User Operator { get; set; } = null!;
    public decimal TotalAmount { get; set; }                     // 总金额
    public decimal? DiscountAmount { get; set; }                 // 优惠金额
    public decimal ActualAmount { get; set; }                    // 实收金额
    public string PaymentMethod { get; set; } = "现金";          // 支付方式: 现金/微信/支付宝/银行卡/医保
    public string Status { get; set; } = "已支付";               // 已支付/已退款
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RefundedAt { get; set; }

    // Navigation
    public ICollection<PaymentItem> Items { get; set; } = new List<PaymentItem>();
}

/// <summary>
/// 收费明细（关联处方、检查等）
/// </summary>
public class PaymentItem
{
    public int Id { get; set; }
    public int PaymentId { get; set; }
    public Payment Payment { get; set; } = null!;
    public string ItemType { get; set; } = string.Empty;         // 类型: 挂号费/药品费/检查费/治疗费
    public int? ItemId { get; set; }                             // 关联ID（处方ID/ChargeItem ID）
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }
}

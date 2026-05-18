using ClinicSystem.Domain.Entities;
using ClinicSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public PaymentsController(AppDbContext db) => _db = db;

    /// <summary>
    /// 收费记录列表
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> GetAll(
        [FromQuery] DateTime? date,
        [FromQuery] string? paymentMethod,
        [FromQuery] string? status,
        [FromQuery] string? keyword,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.Payments
            .Include(p => p.Patient)
            .Include(p => p.Operator)
            .AsQueryable();

        if (date.HasValue)
        {
            var d = date.Value.Date;
            query = query.Where(p => p.CreatedAt >= d && p.CreatedAt < d.AddDays(1));
        }
        if (!string.IsNullOrWhiteSpace(paymentMethod))
            query = query.Where(p => p.PaymentMethod == paymentMethod);
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(p => p.Status == status);
        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(p => p.PaymentNo.Contains(keyword) || p.Patient.Name.Contains(keyword));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                p.Id, p.PaymentNo, p.TotalAmount, p.DiscountAmount,
                p.ActualAmount, p.PaymentMethod, p.Status, p.Remark,
                p.CreatedAt, p.RefundedAt,
                PatientId = p.PatientId, PatientName = p.Patient.Name,
                OperatorName = p.Operator.DisplayName
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    /// <summary>
    /// 获取收费详情（含明细）
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult> GetById(int id)
    {
        var payment = await _db.Payments
            .Include(p => p.Patient)
            .Include(p => p.Operator)
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (payment == null) return NotFound();
        return Ok(payment);
    }

    /// <summary>
    /// 创建收费单（从处方划价）
    /// </summary>
    [HttpPost("from-prescription")]
    [Authorize(Roles = "Admin,Cashier")]
    public async Task<ActionResult> CreateFromPrescription([FromBody] CreatePaymentRequest request)
    {
        var todayStr = DateTime.UtcNow.ToString("yyyyMMdd");
        var count = await _db.Payments.CountAsync(p => p.PaymentNo.StartsWith("PAY" + todayStr));
        var paymentNo = $"PAY{todayStr}{count + 1:D4}";

        var items = new List<PaymentItem>();
        decimal totalAmount = 0;

        if (request.PrescriptionId.HasValue)
        {
            var prescription = await _db.Prescriptions
                .Include(p => p.Items)
                .Include(p => p.MedicalRecord)
                .FirstOrDefaultAsync(p => p.Id == request.PrescriptionId.Value);

            if (prescription == null) return BadRequest(new { message = "处方不存在" });

            foreach (var pi in prescription.Items)
            {
                items.Add(new PaymentItem
                {
                    ItemType = "药品费",
                    ItemId = pi.Id,
                    ItemName = pi.DrugName,
                    Quantity = (int)Math.Ceiling(pi.Quantity),
                    UnitPrice = pi.UnitPrice,
                    SubTotal = pi.SubTotal
                });
                totalAmount += pi.SubTotal;
            }
        }

        if (request.ExtraItems != null)
        {
            foreach (var ei in request.ExtraItems)
            {
                items.Add(new PaymentItem
                {
                    ItemType = ei.ItemType,
                    ItemId = ei.ItemId,
                    ItemName = ei.ItemName,
                    Quantity = ei.Quantity,
                    UnitPrice = ei.UnitPrice,
                    SubTotal = ei.Quantity * ei.UnitPrice
                });
                totalAmount += ei.Quantity * ei.UnitPrice;
            }
        }

        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        var payment = new Payment
        {
            PaymentNo = paymentNo,
            PatientId = request.PatientId,
            AppointmentId = request.AppointmentId,
            OperatorId = userId,
            TotalAmount = totalAmount,
            DiscountAmount = request.DiscountAmount ?? 0,
            ActualAmount = totalAmount - (request.DiscountAmount ?? 0),
            PaymentMethod = request.PaymentMethod,
            Status = "已支付",
            Remark = request.Remark
        };
        payment.Items = items;

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        payment = await _db.Payments
            .Include(p => p.Patient)
            .Include(p => p.Operator)
            .Include(p => p.Items)
            .FirstAsync(p => p.Id == payment.Id);

        return CreatedAtAction(nameof(GetById), new { id = payment.Id }, payment);
    }

    /// <summary>
    /// 退费
    /// </summary>
    [HttpPut("{id}/refund")]
    [Authorize(Roles = "Admin,Cashier")]
    public async Task<ActionResult> Refund(int id, [FromBody] RefundRequest? request)
    {
        var payment = await _db.Payments.FindAsync(id);
        if (payment == null) return NotFound();
        if (payment.Status == "已退款") return BadRequest(new { message = "已退款，不可重复操作" });

        payment.Status = "已退款";
        payment.RefundedAt = DateTime.UtcNow;
        payment.Remark = (payment.Remark ?? "") + " [已退款:" + (request?.Reason ?? "") + "]";
        await _db.SaveChangesAsync();
        return Ok(new { message = "退费成功" });
    }

    /// <summary>
    /// 日结统计
    /// </summary>
    [HttpGet("daily-summary")]
    [Authorize(Roles = "Admin,Cashier,Boss")]
    public async Task<ActionResult> GetDailySummary([FromQuery] DateTime? date)
    {
        var d = (date ?? DateTime.UtcNow).Date;
        var nextDay = d.AddDays(1);

        var payments = await _db.Payments
            .Where(p => p.CreatedAt >= d && p.CreatedAt < nextDay && p.Status == "已支付")
            .ToListAsync();

        var refunded = await _db.Payments
            .Where(p => p.CreatedAt >= d && p.CreatedAt < nextDay && p.Status == "已退款")
            .ToListAsync();

        var summary = payments
            .GroupBy(p => p.PaymentMethod)
            .Select(g => new
            {
                PaymentMethod = g.Key,
                Count = g.Count(),
                TotalAmount = g.Sum(p => p.ActualAmount)
            })
            .ToList();

        return Ok(new
        {
            Date = d,
            TotalCount = payments.Count + refunded.Count,
            PaidCount = payments.Count,
            RefundedCount = refunded.Count,
            TotalIncome = payments.Sum(p => p.ActualAmount),
            TotalRefunded = refunded.Sum(p => p.ActualAmount),
            ByMethod = summary
        });
    }
}

public class CreatePaymentRequest
{
    public int? PrescriptionId { get; set; }
    public int PatientId { get; set; }
    public int? AppointmentId { get; set; }
    public List<ExtraPaymentItem>? ExtraItems { get; set; }
    public decimal? DiscountAmount { get; set; }
    public string PaymentMethod { get; set; } = "现金";
    public string? Remark { get; set; }
}

public class ExtraPaymentItem
{
    public string ItemType { get; set; } = string.Empty;
    public int? ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
}

public class RefundRequest
{
    public string? Reason { get; set; }
}

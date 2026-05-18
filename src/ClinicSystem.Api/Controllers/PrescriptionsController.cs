using ClinicSystem.Domain.Entities;
using ClinicSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PrescriptionsController : ControllerBase
{
    private readonly AppDbContext _db;

    public PrescriptionsController(AppDbContext db) => _db = db;

    /// <summary>
    /// 处方列表
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> GetAll(
        [FromQuery] int? medicalRecordId,
        [FromQuery] int? patientId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.Prescriptions
            .Include(p => p.MedicalRecord).ThenInclude(m => m.Patient)
            .Include(p => p.MedicalRecord).ThenInclude(m => m.Doctor)
            .AsQueryable();

        if (medicalRecordId.HasValue)
            query = query.Where(p => p.MedicalRecordId == medicalRecordId.Value);
        if (patientId.HasValue)
            query = query.Where(p => p.MedicalRecord.PatientId == patientId.Value);
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(p => p.Status == status);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                p.Id, p.PrescriptionNo, p.Type, p.Status,
                p.TotalAmount, p.CreatedAt, p.DispensedAt,
                PatientName = p.MedicalRecord.Patient.Name,
                DoctorName = p.MedicalRecord.Doctor.DisplayName
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    /// <summary>
    /// 获取单个处方详情（含明细）
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult> GetById(int id)
    {
        var prescription = await _db.Prescriptions
            .Include(p => p.MedicalRecord).ThenInclude(m => m.Patient)
            .Include(p => p.MedicalRecord).ThenInclude(m => m.Doctor)
            .Include(p => p.Items).ThenInclude(i => i.Drug)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (prescription == null) return NotFound();
        return Ok(prescription);
    }

    /// <summary>
    /// 创建处方（含明细）
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Doctor")]
    public async Task<ActionResult> Create([FromBody] CreatePrescriptionRequest request)
    {
        var todayStr = DateTime.UtcNow.ToString("yyyyMMdd");
        var count = await _db.Prescriptions.CountAsync(p => p.PrescriptionNo.StartsWith("RX" + todayStr));
        var prescriptionNo = $"RX{todayStr}{count + 1:D4}";

        var totalAmount = request.Items.Sum(i => i.Quantity * i.UnitPrice);

        var prescription = new Prescription
        {
            PrescriptionNo = prescriptionNo,
            MedicalRecordId = request.MedicalRecordId,
            Type = request.Type,
            DecoctingMethod = request.DecoctingMethod,
            Direction = request.Direction,
            TotalAmount = totalAmount,
            Status = "待发药"
        };

        foreach (var item in request.Items)
        {
            prescription.Items.Add(new PrescriptionItem
            {
                DrugId = item.DrugId,
                DrugName = item.DrugName,
                Specification = item.Specification,
                Quantity = item.Quantity,
                Dosage = item.Dosage,
                Frequency = item.Frequency,
                Usage = item.Usage,
                Days = item.Days,
                Remark = item.Remark,
                UnitPrice = item.UnitPrice,
                SubTotal = item.Quantity * item.UnitPrice
            });
        }

        _db.Prescriptions.Add(prescription);
        await _db.SaveChangesAsync();

        prescription = await _db.Prescriptions
            .Include(p => p.Items).ThenInclude(i => i.Drug)
            .FirstAsync(p => p.Id == prescription.Id);

        return CreatedAtAction(nameof(GetById), new { id = prescription.Id }, prescription);
    }

    /// <summary>
    /// 获取待发药处方列表（药房用）
    /// </summary>
    [HttpGet("pending-dispense")]
    [Authorize(Roles = "Admin,Pharmacist")]
    public async Task<ActionResult> GetPendingDispense([FromQuery] string? type, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = _db.Prescriptions
            .Include(p => p.MedicalRecord).ThenInclude(m => m.Patient)
            .Include(p => p.MedicalRecord).ThenInclude(m => m.Doctor)
            .Where(p => p.Status == "待发药")
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(p => p.Type == type);

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                p.Id, p.PrescriptionNo, p.Type, p.TotalAmount,
                p.DecoctingMethod, p.Direction, p.CreatedAt,
                PatientName = p.MedicalRecord.Patient.Name,
                DoctorName = p.MedicalRecord.Doctor.DisplayName
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    /// <summary>
    /// 发药确认
    /// </summary>
    [HttpPut("{id}/dispense")]
    [Authorize(Roles = "Admin,Pharmacist")]
    public async Task<ActionResult> Dispense(int id)
    {
        var prescription = await _db.Prescriptions
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (prescription == null) return NotFound();
        if (prescription.Status != "待发药") return BadRequest(new { message = "当前状态不能发药" });

        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        prescription.Status = "已发药";
        prescription.PharmacistId = userId;
        prescription.DispensedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { message = "发药成功" });
    }

    /// <summary>
    /// 退药
    /// </summary>
    [HttpPut("{id}/return")]
    [Authorize(Roles = "Admin,Pharmacist")]
    public async Task<ActionResult> ReturnDrug(int id)
    {
        var prescription = await _db.Prescriptions.FindAsync(id);
        if (prescription == null) return NotFound();
        if (prescription.Status != "已发药") return BadRequest(new { message = "当前状态不能退药" });

        prescription.Status = "已退药";
        await _db.SaveChangesAsync();
        return Ok(new { message = "退药成功" });
    }
}

public class CreatePrescriptionRequest
{
    public int MedicalRecordId { get; set; }
    public string Type { get; set; } = "西药";
    public string? DecoctingMethod { get; set; }
    public string? Direction { get; set; }
    public List<CreatePrescriptionItemRequest> Items { get; set; } = new();
}

public class CreatePrescriptionItemRequest
{
    public int DrugId { get; set; }
    public string DrugName { get; set; } = string.Empty;
    public string? Specification { get; set; }
    public decimal Quantity { get; set; }
    public string? Dosage { get; set; }
    public string? Frequency { get; set; }
    public string? Usage { get; set; }
    public string? Days { get; set; }
    public string? Remark { get; set; }
    public decimal UnitPrice { get; set; }
}

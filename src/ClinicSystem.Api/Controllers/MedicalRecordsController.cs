using ClinicSystem.Domain.Entities;
using ClinicSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MedicalRecordsController : ControllerBase
{
    private readonly AppDbContext _db;

    public MedicalRecordsController(AppDbContext db) => _db = db;

    /// <summary>
    /// 病历列表（按患者查询）
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> GetAll([FromQuery] int? patientId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = _db.MedicalRecords
            .Include(m => m.Patient)
            .Include(m => m.Doctor)
            .AsQueryable();

        if (patientId.HasValue)
            query = query.Where(m => m.PatientId == patientId.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(m => m.VisitDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new
            {
                m.Id, m.RecordNo, m.VisitDate, m.ChiefComplaint,
                m.WesternDiagnosis, m.TcmDiagnosis, m.TcmSyndrome,
                m.CreatedAt,
                PatientId = m.PatientId, PatientName = m.Patient.Name,
                DoctorId = m.DoctorId, DoctorName = m.Doctor.DisplayName
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    /// <summary>
    /// 获取单个病历详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult> GetById(int id)
    {
        var record = await _db.MedicalRecords
            .Include(m => m.Patient)
            .Include(m => m.Doctor)
            .Include(m => m.Appointment)
            .Include(m => m.Prescriptions).ThenInclude(p => p.Items).ThenInclude(i => i.Drug)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (record == null) return NotFound();
        return Ok(record);
    }

    /// <summary>
    /// 创建电子病历
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Doctor")]
    public async Task<ActionResult> Create([FromBody] CreateMedicalRecordRequest request)
    {
        var todayStr = DateTime.UtcNow.ToString("yyyyMMdd");
        var count = await _db.MedicalRecords.CountAsync(m => m.RecordNo.StartsWith("MR" + todayStr));
        var recordNo = $"MR{todayStr}{count + 1:D4}";

        var record = new MedicalRecord
        {
            RecordNo = recordNo,
            PatientId = request.PatientId,
            DoctorId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value),
            AppointmentId = request.AppointmentId,
            VisitDate = DateTime.UtcNow,
            ChiefComplaint = request.ChiefComplaint,
            PresentIllness = request.PresentIllness,
            PastHistory = request.PastHistory,
            PhysicalExamination = request.PhysicalExamination,
            AuxiliaryExamination = request.AuxiliaryExamination,
            TcmObservation = request.TcmObservation,
            TcmAuscultation = request.TcmAuscultation,
            TcmInquiry = request.TcmInquiry,
            TcmPalpation = request.TcmPalpation,
            WesternDiagnosis = request.WesternDiagnosis,
            TcmDiagnosis = request.TcmDiagnosis,
            TcmSyndrome = request.TcmSyndrome,
            DoctorAdvice = request.DoctorAdvice,
            Remark = request.Remark
        };

        _db.MedicalRecords.Add(record);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = record.Id }, record);
    }

    /// <summary>
    /// 编辑病历
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Doctor")]
    public async Task<ActionResult> Update(int id, [FromBody] CreateMedicalRecordRequest request)
    {
        var record = await _db.MedicalRecords.FindAsync(id);
        if (record == null) return NotFound();

        record.ChiefComplaint = request.ChiefComplaint;
        record.PresentIllness = request.PresentIllness;
        record.PastHistory = request.PastHistory;
        record.PhysicalExamination = request.PhysicalExamination;
        record.AuxiliaryExamination = request.AuxiliaryExamination;
        record.TcmObservation = request.TcmObservation;
        record.TcmAuscultation = request.TcmAuscultation;
        record.TcmInquiry = request.TcmInquiry;
        record.TcmPalpation = request.TcmPalpation;
        record.WesternDiagnosis = request.WesternDiagnosis;
        record.TcmDiagnosis = request.TcmDiagnosis;
        record.TcmSyndrome = request.TcmSyndrome;
        record.DoctorAdvice = request.DoctorAdvice;
        record.Remark = request.Remark;
        record.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { message = "病历更新成功" });
    }
}

public class CreateMedicalRecordRequest
{
    public int PatientId { get; set; }
    public int AppointmentId { get; set; }
    public string? ChiefComplaint { get; set; }
    public string? PresentIllness { get; set; }
    public string? PastHistory { get; set; }
    public string? PhysicalExamination { get; set; }
    public string? AuxiliaryExamination { get; set; }
    public string? TcmObservation { get; set; }
    public string? TcmAuscultation { get; set; }
    public string? TcmInquiry { get; set; }
    public string? TcmPalpation { get; set; }
    public string? WesternDiagnosis { get; set; }
    public string? TcmDiagnosis { get; set; }
    public string? TcmSyndrome { get; set; }
    public string? DoctorAdvice { get; set; }
    public string? Remark { get; set; }
}

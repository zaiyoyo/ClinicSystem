using ClinicSystem.Domain.Entities;
using ClinicSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PatientsController : ControllerBase
{
    private readonly AppDbContext _db;

    public PatientsController(AppDbContext db) => _db = db;

    /// <summary>
    /// 患者列表（分页+搜索）
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> GetAll(
        [FromQuery] string? keyword,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.Patients.AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(p =>
                p.Name.Contains(keyword) ||
                p.Phone!.Contains(keyword) ||
                p.IdCard!.Contains(keyword));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                p.Id, p.Name, p.Gender, p.DateOfBirth,
                p.Phone, p.IdCard, p.Address,
                p.Allergies, p.MedicalHistory,
                p.CreatedAt
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    /// <summary>
    /// 获取单个患者
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult> GetById(int id)
    {
        var patient = await _db.Patients.FindAsync(id);
        if (patient == null) return NotFound();
        return Ok(patient);
    }

    /// <summary>
    /// 新增患者
    /// </summary>
    [HttpPost]
    public async Task<ActionResult> Create([FromBody] Patient request)
    {
        request.CreatedAt = DateTime.UtcNow;
        request.UpdatedAt = DateTime.UtcNow;

        _db.Patients.Add(request);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = request.Id }, request);
    }

    /// <summary>
    /// 编辑患者
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, [FromBody] Patient request)
    {
        var patient = await _db.Patients.FindAsync(id);
        if (patient == null) return NotFound();

        patient.Name = request.Name;
        patient.Gender = request.Gender;
        patient.DateOfBirth = request.DateOfBirth;
        patient.Phone = request.Phone;
        patient.IdCard = request.IdCard;
        patient.Address = request.Address;
        patient.MedicalInsuranceNo = request.MedicalInsuranceNo;
        patient.Allergies = request.Allergies;
        patient.MedicalHistory = request.MedicalHistory;
        patient.Remark = request.Remark;
        patient.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { message = "更新成功" });
    }

    /// <summary>
    /// 删除患者（软删除）
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Delete(int id)
    {
        var patient = await _db.Patients.FindAsync(id);
        if (patient == null) return NotFound();

        patient.IsDeleted = true;
        await _db.SaveChangesAsync();
        return Ok(new { message = "删除成功" });
    }

    /// <summary>
    /// 合并患者：将源患者数据合并到目标患者，并软删除源患者
    /// </summary>
    [HttpPost("merge")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Merge([FromBody] MergePatientRequest request)
    {
        var source = await _db.Patients.FindAsync(request.SourceId);
        var target = await _db.Patients.FindAsync(request.TargetId);
        if (source == null || target == null) return NotFound(new { message = "患者不存在" });

        var appointments = await _db.Appointments.Where(a => a.PatientId == request.SourceId).ToListAsync();
        foreach (var apt in appointments)
            apt.PatientId = request.TargetId;

        var records = await _db.MedicalRecords.Where(m => m.PatientId == request.SourceId).ToListAsync();
        foreach (var r in records)
            r.PatientId = request.TargetId;

        var members = await _db.FamilyMembers.Where(f => f.PatientId == request.SourceId).ToListAsync();
        foreach (var m in members)
            m.PatientId = request.TargetId;

        source.IsDeleted = true;
        await _db.SaveChangesAsync();
        return Ok(new { message = "患者合并成功" });
    }

    // ===== 家庭成员 =====

    /// <summary>
    /// 获取患者的家庭成员列表
    /// </summary>
    [HttpGet("{patientId}/family")]
    public async Task<ActionResult> GetFamilyMembers(int patientId)
    {
        var members = await _db.FamilyMembers
            .Where(f => f.PatientId == patientId)
            .OrderBy(f => f.CreatedAt)
            .ToListAsync();
        return Ok(members);
    }

    /// <summary>
    /// 添加家庭成员
    /// </summary>
    [HttpPost("{patientId}/family")]
    public async Task<ActionResult> AddFamilyMember(int patientId, [FromBody] AddFamilyMemberRequest dto)
    {
        var member = new FamilyMember
        {
            PatientId = patientId,
            Name = dto.Name,
            Relationship = dto.Relationship,
            Phone = dto.Phone,
            IdCard = dto.IdCard,
            CreatedAt = DateTime.UtcNow
        };
        _db.FamilyMembers.Add(member);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetFamilyMembers), new { patientId }, new
        {
            member.Id, member.Name, member.Relationship, member.Phone, member.IdCard
        });
    }

    /// <summary>
    /// 编辑家庭成员
    /// </summary>
    [HttpPut("{patientId}/family/{memberId}")]
    public async Task<ActionResult> UpdateFamilyMember(int patientId, int memberId, [FromBody] AddFamilyMemberRequest dto)
    {
        var member = await _db.FamilyMembers.FirstOrDefaultAsync(f => f.Id == memberId && f.PatientId == patientId);
        if (member == null) return NotFound();

        member.Name = dto.Name;
        member.Relationship = dto.Relationship;
        member.Phone = dto.Phone;
        member.IdCard = dto.IdCard;
        await _db.SaveChangesAsync();
        return Ok(new { message = "更新成功" });
    }

    /// <summary>
    /// 删除家庭成员
    /// </summary>
    [HttpDelete("{patientId}/family/{memberId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> DeleteFamilyMember(int patientId, int memberId)
    {
        var member = await _db.FamilyMembers.FirstOrDefaultAsync(f => f.Id == memberId && f.PatientId == patientId);
        if (member == null) return NotFound();

        _db.FamilyMembers.Remove(member);
        await _db.SaveChangesAsync();
        return Ok(new { message = "删除成功" });
    }
}

public class MergePatientRequest
{
    public int SourceId { get; set; }
    public int TargetId { get; set; }
}

public class AddFamilyMemberRequest
{
    public string Name { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? IdCard { get; set; }
}

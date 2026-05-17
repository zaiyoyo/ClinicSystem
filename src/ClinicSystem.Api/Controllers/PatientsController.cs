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
}

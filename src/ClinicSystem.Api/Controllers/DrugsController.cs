using ClinicSystem.Domain.Entities;
using ClinicSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DrugsController : ControllerBase
{
    private readonly AppDbContext _db;

    public DrugsController(AppDbContext db) => _db = db;

    /// <summary>
    /// 药品列表（分页+搜索）
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> GetAll(
        [FromQuery] string? keyword,
        [FromQuery] string? category,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.Drugs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(d => d.Name.Contains(keyword) || d.Code.Contains(keyword) || d.CommonName!.Contains(keyword));

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(d => d.Category == category);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    /// <summary>
    /// 获取所有活跃的药品（用于下拉选择）
    /// </summary>
    [HttpGet("active")]
    public async Task<ActionResult> GetActive()
    {
        var items = await _db.Drugs
            .Where(d => d.IsActive)
            .OrderBy(d => d.Category)
            .ThenBy(d => d.Name)
            .Select(d => new { d.Id, d.Code, d.Name, d.Specification, d.Manufacturer, d.Category, d.DosageForm, d.Unit, d.Price, d.IsPrescription })
            .ToListAsync();
        return Ok(items);
    }

    /// <summary>
    /// 获取单个药品
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult> GetById(int id)
    {
        var drug = await _db.Drugs.FindAsync(id);
        if (drug == null) return NotFound();
        return Ok(drug);
    }

    /// <summary>
    /// 新增药品
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Create([FromBody] Drug request)
    {
        if (await _db.Drugs.AnyAsync(d => d.Code == request.Code))
            return BadRequest(new { message = "药品编码已存在" });

        request.CreatedAt = DateTime.UtcNow;
        _db.Drugs.Add(request);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = request.Id }, request);
    }

    /// <summary>
    /// 编辑药品
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Update(int id, [FromBody] Drug request)
    {
        var drug = await _db.Drugs.FindAsync(id);
        if (drug == null) return NotFound();

        drug.Name = request.Name;
        drug.CommonName = request.CommonName;
        drug.Specification = request.Specification;
        drug.Manufacturer = request.Manufacturer;
        drug.Category = request.Category;
        drug.DosageForm = request.DosageForm;
        drug.Unit = request.Unit;
        drug.Price = request.Price;
        drug.CostPrice = request.CostPrice;
        drug.IsPrescription = request.IsPrescription;
        drug.IsActive = request.IsActive;
        drug.Remark = request.Remark;

        await _db.SaveChangesAsync();
        return Ok(new { message = "更新成功" });
    }

    /// <summary>
    /// 删除药品（软删除）
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Delete(int id)
    {
        var drug = await _db.Drugs.FindAsync(id);
        if (drug == null) return NotFound();

        drug.IsActive = false;
        await _db.SaveChangesAsync();
        return Ok(new { message = "删除成功" });
    }
}

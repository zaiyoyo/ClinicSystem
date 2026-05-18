using ClinicSystem.Domain.Entities;
using ClinicSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChargeItemsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ChargeItemsController(AppDbContext db) => _db = db;

    /// <summary>
    /// 收费项目列表（分页+搜索）
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> GetAll(
        [FromQuery] string? keyword,
        [FromQuery] string? category,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.ChargeItems.AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(c => c.Name.Contains(keyword) || c.Code.Contains(keyword));

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(c => c.Category == category);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    /// <summary>
    /// 获取所有活跃的收费项目（用于下拉选择）
    /// </summary>
    [HttpGet("active")]
    public async Task<ActionResult> GetActive()
    {
        var items = await _db.ChargeItems
            .Where(c => c.IsActive)
            .OrderBy(c => c.Category)
            .ThenBy(c => c.Name)
            .Select(c => new { c.Id, c.Code, c.Name, c.Category, c.Price, c.Unit })
            .ToListAsync();
        return Ok(items);
    }

    /// <summary>
    /// 获取单个收费项目
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult> GetById(int id)
    {
        var item = await _db.ChargeItems.FindAsync(id);
        if (item == null) return NotFound();
        return Ok(item);
    }

    /// <summary>
    /// 新增收费项目
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Create([FromBody] ChargeItem request)
    {
        if (await _db.ChargeItems.AnyAsync(c => c.Code == request.Code))
            return BadRequest(new { message = "项目编码已存在" });

        request.CreatedAt = DateTime.UtcNow;
        _db.ChargeItems.Add(request);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = request.Id }, request);
    }

    /// <summary>
    /// 编辑收费项目
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Update(int id, [FromBody] ChargeItem request)
    {
        var item = await _db.ChargeItems.FindAsync(id);
        if (item == null) return NotFound();

        item.Name = request.Name;
        item.Category = request.Category;
        item.Price = request.Price;
        item.Unit = request.Unit;
        item.IsActive = request.IsActive;
        item.Remark = request.Remark;

        await _db.SaveChangesAsync();
        return Ok(new { message = "更新成功" });
    }

    /// <summary>
    /// 删除收费项目（软删除）
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Delete(int id)
    {
        var item = await _db.ChargeItems.FindAsync(id);
        if (item == null) return NotFound();

        item.IsActive = false;
        await _db.SaveChangesAsync();
        return Ok(new { message = "删除成功" });
    }
}

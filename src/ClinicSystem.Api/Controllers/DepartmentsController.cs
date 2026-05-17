using ClinicSystem.Domain.Entities;
using ClinicSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DepartmentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public DepartmentsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult> GetAll()
    {
        var items = await _db.Departments
            .Where(d => d.IsActive)
            .OrderBy(d => d.SortOrder)
            .Select(d => new { d.Id, d.Name, d.Description, d.SortOrder })
            .ToListAsync();
        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult> GetById(int id)
    {
        var dept = await _db.Departments.FindAsync(id);
        if (dept == null) return NotFound();
        return Ok(dept);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Create([FromBody] Department request)
    {
        request.CreatedAt = DateTime.UtcNow;
        _db.Departments.Add(request);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = request.Id }, request);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Update(int id, [FromBody] Department request)
    {
        var dept = await _db.Departments.FindAsync(id);
        if (dept == null) return NotFound();
        dept.Name = request.Name;
        dept.Description = request.Description;
        dept.SortOrder = request.SortOrder;
        dept.IsActive = request.IsActive;
        await _db.SaveChangesAsync();
        return Ok(new { message = "更新成功" });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Delete(int id)
    {
        var dept = await _db.Departments.FindAsync(id);
        if (dept == null) return NotFound();
        dept.IsActive = false;
        await _db.SaveChangesAsync();
        return Ok(new { message = "删除成功" });
    }
}

using ClinicSystem.Domain.Entities;
using ClinicSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// 用户列表（分页）
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = _db.Users.Include(u => u.Department).AsQueryable();
        var total = await query.CountAsync();
        var items = await query
            .OrderBy(u => u.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.Id,
                u.Username,
                u.DisplayName,
                u.Phone,
                Role = u.Role.ToString(),
                DepartmentName = u.Department!.Name,
                u.IsActive,
                u.Title,
                u.LastLoginAt,
                u.CreatedAt
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    /// <summary>
    /// 获取单个用户
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult> GetById(int id)
    {
        var user = await _db.Users.Include(u => u.Department).FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();
        return Ok(user);
    }

    /// <summary>
    /// 创建用户（管理员）
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Create([FromBody] CreateUserRequest request)
    {
        if (await _db.Users.AnyAsync(u => u.Username == request.Username))
            return BadRequest(new { message = "用户名已存在" });

        var user = new User
        {
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            DisplayName = request.DisplayName,
            Phone = request.Phone,
            Role = request.Role,
            Title = request.Title,
            DepartmentId = request.DepartmentId,
            ConsultationFee = request.ConsultationFee,
            MaxDailyPatients = request.MaxDailyPatients
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    /// <summary>
    /// 更新用户
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Update(int id, [FromBody] UpdateUserRequest request)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();

        user.DisplayName = request.DisplayName;
        user.Phone = request.Phone;
        user.Role = request.Role;
        user.Title = request.Title;
        user.DepartmentId = request.DepartmentId;
        user.IsActive = request.IsActive;
        user.ConsultationFee = request.ConsultationFee;
        user.MaxDailyPatients = request.MaxDailyPatients;

        if (!string.IsNullOrEmpty(request.Password))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        await _db.SaveChangesAsync();
        return Ok(new { message = "更新成功" });
    }
}

// Request DTOs for user operations
public class CreateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public Domain.Enums.UserRole Role { get; set; }
    public string? Title { get; set; }
    public int? DepartmentId { get; set; }
    public decimal? ConsultationFee { get; set; }
    public int? MaxDailyPatients { get; set; }
}

public class UpdateUserRequest
{
    public string DisplayName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public Domain.Enums.UserRole Role { get; set; }
    public string? Title { get; set; }
    public int? DepartmentId { get; set; }
    public bool IsActive { get; set; } = true;
    public decimal? ConsultationFee { get; set; }
    public int? MaxDailyPatients { get; set; }
    public string? Password { get; set; } // 为空则不修改密码
}

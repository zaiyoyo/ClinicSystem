using ClinicSystem.Application.DTOs.Auth;
using ClinicSystem.Application.Interfaces;
using ClinicSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ITokenService _tokenService;
    private readonly AppDbContext _db;

    public AuthController(IAuthService authService, ITokenService tokenService, AppDbContext db)
    {
        _authService = authService;
        _tokenService = tokenService;
        _db = db;
    }

    /// <summary>
    /// 用户登录，验证用户名密码并返回JWT Token
    /// </summary>
    /// <param name="request">登录请求，包含用户名和密码</param>
    /// <returns>登录响应，包含Token、过期时间和用户信息</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await _authService.AuthenticateAsync(request.Username, request.Password);
        if (user == null)
            return Unauthorized(new { message = "用户名或密码错误" });

        if (!user.IsActive)
            return Unauthorized(new { message = "账号已被禁用，请联系管理员" });

        var token = _tokenService.GenerateToken(user);
        var expiresAt = DateTime.UtcNow.AddHours(12);

        return Ok(new LoginResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = new UserInfo
            {
                Id = user.Id,
                Username = user.Username,
                DisplayName = user.DisplayName,
                Role = user.Role.ToString(),
                Phone = user.Phone,
                Department = user.Department?.Name,
                DepartmentId = user.DepartmentId
            }
        });
    }

    /// <summary>
    /// 获取当前登录用户信息
    /// </summary>
    /// <returns>当前用户信息</returns>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserInfo>> GetCurrentUser()
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var user = await _db.Users.Include(u => u.Department).FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return NotFound(new { message = "用户不存在" });

        return Ok(new UserInfo
        {
            Id = user.Id,
            Username = user.Username,
            DisplayName = user.DisplayName,
            Role = user.Role.ToString(),
            Phone = user.Phone,
            Department = user.Department?.Name,
            DepartmentId = user.DepartmentId
        });
    }

    /// <summary>
    /// 修改当前用户密码
    /// </summary>
    /// <param name="request">包含旧密码和新密码的请求体</param>
    /// <returns>操作结果</returns>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound(new { message = "用户不存在" });

        if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash))
            return BadRequest(new { message = "原密码错误" });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _db.SaveChangesAsync();

        return Ok(new { message = "密码修改成功" });
    }
}

using ClinicSystem.Application.DTOs.Auth;
using ClinicSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ITokenService _tokenService;

    public AuthController(IAuthService authService, ITokenService tokenService)
    {
        _authService = authService;
        _tokenService = tokenService;
    }

    /// <summary>
    /// 用户登录
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await _authService.AuthenticateAsync(request.Username, request.Password);
        if (user == null)
            return Unauthorized(new { message = "用户名或密码错误" });

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
                Department = user.Department?.Name
            }
        });
    }

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public ActionResult<UserInfo> GetCurrentUser()
    {
        var user = new UserInfo
        {
            Id = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value),
            Username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)!.Value,
            DisplayName = User.FindFirst(System.Security.Claims.ClaimTypes.GivenName)!.Value,
            Role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)!.Value,
            Department = User.FindFirst("department")?.Value
        };
        return Ok(user);
    }
}

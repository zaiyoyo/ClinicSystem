using ClinicSystem.Application.Interfaces;
using ClinicSystem.Domain.Entities;
using ClinicSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClinicSystem.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;

    public AuthService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<User?> AuthenticateAsync(string username, string password)
    {
        var user = await _db.Users
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

        if (user == null) return null;

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return null;

        // 更新最后登录时间
        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return user;
    }
}

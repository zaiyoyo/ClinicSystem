using ClinicSystem.Domain.Entities;

namespace ClinicSystem.Application.Interfaces;

public interface ITokenService
{
    string GenerateToken(User user);
}

public interface IAuthService
{
    Task<Domain.Entities.User?> AuthenticateAsync(string username, string password);
}

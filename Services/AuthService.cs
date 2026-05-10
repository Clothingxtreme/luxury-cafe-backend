using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using MaisonGlace.API.Models;
using MaisonGlace.API.Settings;
using Microsoft.Extensions.Options;

namespace MaisonGlace.API.Services;

public class AuthService
{
    private readonly IMongoCollection<AdminUser> _admins;
    private readonly JwtSettings _jwt;

    public AuthService(DatabaseContext db, IOptions<JwtSettings> jwtOptions)
    {
        _admins = db.GetCollection<AdminUser>("admins");
        _jwt = jwtOptions.Value;
    }

    /// <summary>Seeds the admin account on first startup if it does not already exist.</summary>
    public async Task SeedAdminAsync(string email, string plainPassword)
    {
        var exists = await _admins.Find(a => a.Email == email).AnyAsync();
        if (!exists)
        {
            await _admins.InsertOneAsync(new AdminUser
            {
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword),
            });
        }
    }

    /// <summary>Returns a JWT token on success, null on bad credentials.</summary>
    public async Task<string?> LoginAsync(string email, string plainPassword)
    {
        var admin = await _admins.Find(a => a.Email == email).FirstOrDefaultAsync();
        if (admin is null || !BCrypt.Net.BCrypt.Verify(plainPassword, admin.PasswordHash))
            return null;

        return IssueToken(admin);
    }

    // ── Private ───────────────────────────────────────────────────────────

    private string IssueToken(AdminUser admin)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Email, admin.Email),
            new Claim(ClaimTypes.Role, admin.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(_jwt.ExpiryHours),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

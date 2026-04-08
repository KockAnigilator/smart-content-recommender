using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SmartContentRecommender.Application.Auth.Interfaces;
using SmartContentRecommender.Application.Auth.Models;
using SmartContentRecommender.Domain.Entities;
using SmartContentRecommender.Domain.Enums;
using SmartContentRecommender.Infrastructure.Persistence;

namespace SmartContentRecommender.Infrastructure.Auth;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly JwtOptions _jwtOptions;
    private readonly PasswordHasher<User> _passwordHasher = new();

    private static readonly ConcurrentDictionary<string, (int FailCount, DateTimeOffset LastFail)> LoginFails = new();
    private const int MaxFails = 5;
    private static readonly TimeSpan FailWindow = TimeSpan.FromMinutes(5);

    public AuthService(ApplicationDbContext dbContext, IOptions<JwtOptions> jwtOptions)
    {
        _dbContext = dbContext;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return AuthResult.Fail("Email и пароль обязательны.");
        }

        if (!IsPasswordStrong(request.Password))
        {
            return AuthResult.Fail("Пароль должен быть не короче 8 символов и содержать цифру, заглавную букву и спецсимвол.");
        }

        var userExists = await _dbContext.Users.AnyAsync(u => u.Email == email, cancellationToken);
        if (userExists)
        {
            return AuthResult.Fail("Пользователь с таким Email уже существует.");
        }

        var user = new User
        {
            Email = email,
            Role = UserRole.User
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = BuildTokenResponse(user);
        return AuthResult.Success(response, "Регистрация выполнена успешно.");
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return AuthResult.Fail("Email и пароль обязательны.");
        }

        if (IsLockedOut(email))
        {
            return AuthResult.Fail("Слишком много неудачных попыток входа. Попробуйте позже.");
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        if (user is null)
        {
            RegisterFail(email);
            return AuthResult.Fail("Неверный Email или пароль.");
        }

        if (user.IsBlocked)
        {
            return AuthResult.Fail("Пользователь заблокирован администратором.");
        }

        var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verifyResult == PasswordVerificationResult.Failed)
        {
            RegisterFail(email);
            return AuthResult.Fail("Неверный Email или пароль.");
        }

        ResetFail(email);

        var response = BuildTokenResponse(user);
        return AuthResult.Success(response, "Авторизация выполнена успешно.");
    }

    private AuthResponse BuildTokenResponse(User user)
    {
        if (string.IsNullOrWhiteSpace(_jwtOptions.Key))
        {
            throw new InvalidOperationException("JWT ключ не задан в конфигурации.");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAtUtc = DateTime.UtcNow.AddHours(_jwtOptions.ExpiresHours);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return new AuthResponse
        {
            Token = tokenString,
            ExpiresAtUtc = expiresAtUtc
        };
    }

    private static bool IsPasswordStrong(string password)
    {
        if (password.Length < 8)
        {
            return false;
        }

        var hasDigit = password.Any(char.IsDigit);
        var hasUpper = password.Any(char.IsUpper);
        var hasSpecial = password.Any(ch => "!@#$%^&*".Contains(ch));

        return hasDigit && hasUpper && hasSpecial;
    }

    private static bool IsLockedOut(string email)
    {
        if (!LoginFails.TryGetValue(email, out var info))
        {
            return false;
        }

        if (DateTimeOffset.UtcNow - info.LastFail > FailWindow)
        {
            return false;
        }

        return info.FailCount >= MaxFails;
    }

    private static void RegisterFail(string email)
    {
        LoginFails.AddOrUpdate(
            email,
            _ => (1, DateTimeOffset.UtcNow),
            (_, old) => (old.FailCount + 1, DateTimeOffset.UtcNow));
    }

    private static void ResetFail(string email)
    {
        LoginFails.TryRemove(email, out _);
    }
}


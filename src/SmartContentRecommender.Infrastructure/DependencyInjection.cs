using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartContentRecommender.Application.Auth.Interfaces;
using SmartContentRecommender.Infrastructure.Auth;
using SmartContentRecommender.Infrastructure.Persistence;

namespace SmartContentRecommender.Infrastructure;

/// <summary>
/// Регистрация сервисов инфраструктуры (БД, внешние API и т.д.).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Не задана строка подключения 'DefaultConnection'. " +
                "Укажите её в appsettings.json, переменной окружения или user-secrets (не коммитьте пароль).");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));
        services.Configure<JwtOptions>(options =>
            configuration.GetSection(JwtOptions.SectionName).Bind(options));
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}

using Gloria.Commission.Application.Abstractions;
using Gloria.Commission.Application.Rules;
using Gloria.Commission.Application.Rules.Strategies;
using Gloria.Commission.Infrastructure.Import;
using Gloria.Commission.Infrastructure.Persistence;
using Gloria.Commission.Infrastructure.Persistence.Interceptors;
using Gloria.Commission.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gloria.Commission.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCommissionInfrastructure(
        this IServiceCollection services, string connectionString)
    {
        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddScoped<ClosedPeriodGuardInterceptor>();

        services.AddDbContext<CommissionDbContext>((provider, options) =>
        {
            options.UseSqlite(connectionString);
            options.AddInterceptors(
                provider.GetRequiredService<ClosedPeriodGuardInterceptor>(),
                provider.GetRequiredService<AuditSaveChangesInterceptor>());
        });

        // Kural tipleri: yeni bir tip eklemek = yeni bir strateji kaydi.
        services.AddSingleton<ICommissionRuleStrategy, PercentageRuleStrategy>();
        services.AddSingleton<ICommissionRuleStrategy, TieredRuleStrategy>();
        services.AddSingleton<ICommissionRuleStrategy, FixedAmountRuleStrategy>();
        services.AddSingleton<ICommissionCalculator, CommissionCalculator>();

        services.AddSingleton<IExchangeRateProvider, StaticExchangeRateProvider>();

        services.AddScoped<ISourceImporter, PmsImporter>();
        services.AddScoped<ISourceImporter, PosImporter>();
        services.AddScoped<ISourceImporter, ErpImporter>();
        services.AddScoped<IImportService, ImportService>();

        services.AddScoped<ICommissionService, CommissionService>();
        services.AddScoped<IRuleService, RuleService>();
        services.AddScoped<IPeriodService, PeriodService>();
        services.AddScoped<IReconciliationService, ReconciliationService>();

        return services;
    }
}

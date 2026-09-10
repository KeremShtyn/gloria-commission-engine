using Gloria.Commission.Application.Abstractions;
using Gloria.Commission.Application.Import;
using Gloria.Commission.Application.Repositories;
using Gloria.Commission.Application.Rules;
using Gloria.Commission.Application.Rules.Strategies;
using Gloria.Commission.Application.Services;
using Gloria.Commission.Infrastructure.Import;
using Gloria.Commission.Infrastructure.Persistence;
using Gloria.Commission.Infrastructure.Persistence.Interceptors;
using Gloria.Commission.Infrastructure.Repositories;
using Gloria.Commission.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gloria.Commission.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Katmanlarin baglanmasi. Api yalnizca bu metodu cagirir; hangi ORM'in kullanildigini
    /// veya repository'lerin nasil uygulandigini bilmez.
    /// </summary>
    public static IServiceCollection AddCommissionInfrastructure(
        this IServiceCollection services, IConfiguration configuration, string connectionString)
    {
        // --- Veritabani ---
        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddScoped<ClosedPeriodGuardInterceptor>();

        services.AddDbContext<CommissionDbContext>((provider, options) =>
        {
            options.UseSqlite(connectionString);
            options.AddInterceptors(
                provider.GetRequiredService<ClosedPeriodGuardInterceptor>(),
                provider.GetRequiredService<AuditSaveChangesInterceptor>());
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // --- Repository katmani ---
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<ICommissionRuleRepository, CommissionRuleRepository>();
        services.AddScoped<ISaleRecordRepository, SaleRecordRepository>();
        services.AddScoped<ICommissionResultRepository, CommissionResultRepository>();
        services.AddScoped<IPeriodRepository, PeriodRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IImportRepository, ImportRepository>();

        // --- Kural motoru ---
        // Yeni bir kural tipi eklemek = burada yeni bir strateji kaydi.
        // Yeni bir kural eklemek ise yalnizca veri; kod degismez.
        services.AddSingleton<ICommissionRuleStrategy, PercentageRuleStrategy>();
        services.AddSingleton<ICommissionRuleStrategy, TieredRuleStrategy>();
        services.AddSingleton<ICommissionRuleStrategy, FixedAmountRuleStrategy>();
        services.AddSingleton<ICommissionCalculator, CommissionCalculator>();

        // --- Kaynak sistem ayristiricilari ---
        services.AddScoped<ISourceImporter, PmsImporter>();
        services.AddScoped<ISourceImporter, PosImporter>();
        services.AddScoped<ISourceImporter, ErpImporter>();

        services.AddSingleton<IExchangeRateProvider, StaticExchangeRateProvider>();

        // --- Zamanlanmis aktarim ---
        services.Configure<ImportWatcherOptions>(
            configuration.GetSection(ImportWatcherOptions.SectionName));
        services.AddHostedService<ImportWatcherService>();

        // --- Servis katmani ---
        services.AddScoped<IRuleService, RuleService>();
        services.AddScoped<ICommissionService, CommissionService>();
        services.AddScoped<IPeriodService, PeriodService>();
        services.AddScoped<IImportService, ImportService>();
        services.AddScoped<IReconciliationService, ReconciliationService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IReferenceService, ReferenceService>();

        return services;
    }
}

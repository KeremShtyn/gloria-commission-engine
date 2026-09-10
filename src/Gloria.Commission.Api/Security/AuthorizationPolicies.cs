using System.Security.Claims;
using Gloria.Commission.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace Gloria.Commission.Api.Security;

public static class Policies
{
    /// <summary>Kural yonetimi ve donem kapatma.</summary>
    public const string AdminOnly = "AdminOnly";

    /// <summary>Tum personelin verisini gorebilenler: Admin ve Muhasebe.</summary>
    public const string CanSeeAllEmployees = "CanSeeAllEmployees";

    /// <summary>Kendi kaydi ya da ayricalikli rol.</summary>
    public const string SelfOrPrivileged = "SelfOrPrivileged";

    public static void Register(AuthorizationOptions options)
    {
        options.AddPolicy(AdminOnly, policy =>
            policy.RequireRole(nameof(UserRole.Admin)));

        options.AddPolicy(CanSeeAllEmployees, policy =>
            policy.RequireRole(nameof(UserRole.Admin), nameof(UserRole.Accounting)));

        options.AddPolicy(SelfOrPrivileged, policy =>
            policy.AddRequirements(new SelfOrPrivilegedRequirement()));
    }
}

public sealed class SelfOrPrivilegedRequirement : IAuthorizationRequirement;

/// <summary>
/// "Personel yalnizca kendi primini gorur" kurali.
///
/// Rol kontrolu tek basina yetmiyor: hangi personelin sorgulandigi rota degerinde.
/// Bu yuzden basit bir rol nitelgi degil, kaynak farkindaligi olan bir policy gerekiyor.
/// </summary>
public sealed class SelfOrPrivilegedHandler : AuthorizationHandler<SelfOrPrivilegedRequirement>
{
    private const string RouteKey = "employeeNo";

    private readonly IHttpContextAccessor _accessor;

    public SelfOrPrivilegedHandler(IHttpContextAccessor accessor) => _accessor = accessor;

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, SelfOrPrivilegedRequirement requirement)
    {
        if (context.User.IsInRole(nameof(UserRole.Admin))
            || context.User.IsInRole(nameof(UserRole.Accounting)))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var httpContext = context.Resource as HttpContext ?? _accessor.HttpContext;
        var requested = httpContext?.Request.RouteValues[RouteKey]?.ToString();
        var own = context.User.FindFirstValue(AuthenticationHeaders.EmployeeNoClaim);

        if (!string.IsNullOrWhiteSpace(requested)
            && !string.IsNullOrWhiteSpace(own)
            && string.Equals(requested, own, StringComparison.OrdinalIgnoreCase))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

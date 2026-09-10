using System.Security.Claims;
using FluentAssertions;
using Gloria.Commission.Api.Security;
using Gloria.Commission.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Gloria.Commission.UnitTests;

/// <summary>
/// "Personel yalnizca kendi primini gorur" kurali bir guvenlik kontrolu;
/// rol niteliği tek basina yetmedigi icin ayri bir policy olarak test ediliyor.
/// </summary>
public class AuthorizationTests
{
    [Fact]
    public async Task Personel_kendi_kaydina_erisebilir()
    {
        var result = await EvaluateAsync(UserRole.Employee, ownEmployeeNo: "P1001", requested: "P1001");
        result.Should().BeTrue();
    }

    [Fact]
    public async Task Personel_baskasinin_kaydina_erisemez()
    {
        var result = await EvaluateAsync(UserRole.Employee, ownEmployeeNo: "P1001", requested: "P1012");
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Personel_numarasi_olmayan_personel_hicbir_kayda_erisemez()
    {
        var result = await EvaluateAsync(UserRole.Employee, ownEmployeeNo: null, requested: "P1001");
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(UserRole.Admin)]
    [InlineData(UserRole.Accounting)]
    public async Task Ayricalikli_roller_her_kayda_erisebilir(UserRole role)
    {
        var result = await EvaluateAsync(role, ownEmployeeNo: null, requested: "P1012");
        result.Should().BeTrue();
    }

    [Fact]
    public async Task Personel_numarasi_buyuk_kucuk_harf_farkina_takilmaz()
    {
        var result = await EvaluateAsync(UserRole.Employee, ownEmployeeNo: "p1001", requested: "P1001");
        result.Should().BeTrue();
    }

    private static async Task<bool> EvaluateAsync(UserRole role, string? ownEmployeeNo, string requested)
    {
        var claims = new List<Claim> { new(ClaimTypes.Role, role.ToString()) };
        if (ownEmployeeNo is not null)
            claims.Add(new Claim(AuthenticationHeaders.EmployeeNoClaim, ownEmployeeNo));

        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(claims, AuthenticationHeaders.Scheme));

        var httpContext = new DefaultHttpContext { User = principal };
        httpContext.Request.RouteValues["employeeNo"] = requested;

        var requirement = new SelfOrPrivilegedRequirement();
        var context = new AuthorizationHandlerContext([requirement], principal, httpContext);

        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        await new SelfOrPrivilegedHandler(accessor).HandleAsync(context);

        return context.HasSucceeded;
    }
}

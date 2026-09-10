using System.Text.Json.Serialization;
using Gloria.Commission.Api.Endpoints;
using Gloria.Commission.Api.Middleware;
using Gloria.Commission.Api.Security;
using Gloria.Commission.Application.Abstractions;
using Gloria.Commission.Infrastructure;
using Gloria.Commission.Infrastructure.Persistence;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default")
                       ?? "Data Source=gloria-commission.db";

builder.Services.AddCommissionInfrastructure(connectionString);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HeaderCurrentUser>();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Gloria Prim Motoru",
        Version = "v1",
        Description = "Personel prim hesaplama ve kural yonetimi API'si."
    });

    // Rol basligi Swagger uzerinden de denenebilsin.
    options.AddSecurityDefinition(HeaderCurrentUser.RoleHeader, new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = HeaderCurrentUser.RoleHeader,
        Type = SecuritySchemeType.ApiKey,
        Description = "Admin | Accounting | Employee"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = HeaderCurrentUser.RoleHeader
            }
        }] = Array.Empty<string>()
    });
});

const string CorsPolicy = "web";
builder.Services.AddCors(options => options.AddPolicy(CorsPolicy, policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
                 ?? ["http://localhost:5173"])
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseCors(CorsPolicy);

app.UseSwagger();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Gloria Prim Motoru v1"));

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CommissionDbContext>();
    var seedPath = Path.Combine(app.Environment.ContentRootPath, "..", "..", "sample-data", "personel.csv");
    await DbSeeder.SeedAsync(db, Path.GetFullPath(seedPath));
}

app.MapRuleEndpoints();
app.MapCommissionEndpoints();
app.MapImportEndpoints();
app.MapPeriodEndpoints();
app.MapReferenceEndpoints();

app.MapGet("/health", () => Results.Ok(new { status = "ok" })).ExcludeFromDescription();

app.Run();

/// <summary>Entegrasyon testlerinin WebApplicationFactory ile baglanabilmesi icin.</summary>
public partial class Program;

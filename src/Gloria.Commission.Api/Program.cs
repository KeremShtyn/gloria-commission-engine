using System.Text.Json.Serialization;
using Gloria.Commission.Api.Logging;
using Gloria.Commission.Api.Middleware;
using Gloria.Commission.Api.Security;
using Gloria.Commission.Application.Abstractions;
using Gloria.Commission.Infrastructure;
using Gloria.Commission.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog(LoggingSetup.ConfigureSerilog);

var connectionString = builder.Configuration.GetConnectionString("Default")
                       ?? "Data Source=gloria-commission.db";

builder.Services.AddCommissionInfrastructure(builder.Configuration, connectionString);

builder.Services.AddHttpContextAccessor();
// HTTP istegi varsa kullanici claim'lerden, yoksa (zamanlanmis is) sistem kullanicisi.
builder.Services.AddScoped<ICurrentUser>(provider =>
{
    var httpContext = provider.GetRequiredService<IHttpContextAccessor>().HttpContext;
    return httpContext is null
        ? new SystemCurrentUser()
        : new HttpContextCurrentUser(httpContext.User);
});

// Kimlik basliktan okunuyor (case gercek kimlik dogrulama istemiyor);
// yetkilendirme ASP.NET'in kendi altyapisiyla yapiliyor.
// Uretimde asagidaki satir AddJwtBearer ile degistirilir, gerisi ayni kalir.
builder.Services
    .AddAuthentication(AuthenticationHeaders.Scheme)
    .AddScheme<AuthenticationSchemeOptions, HeaderAuthenticationHandler>(
        AuthenticationHeaders.Scheme, _ => { });

builder.Services.AddAuthorization(Policies.Register);
builder.Services.AddSingleton<IAuthorizationHandler, SelfOrPrivilegedHandler>();

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

        // Enum'lar sayi degil isim olarak tasinir: "Percentage", "Pms".
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Model dogrulama hatalari da diger hatalarla ayni bicimde donsun.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = ValidationProblemFactory.Create;
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
    options.AddSecurityDefinition(AuthenticationHeaders.Role, new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = AuthenticationHeaders.Role,
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
                Id = AuthenticationHeaders.Role
            }
        }] = Array.Empty<string>()
    });
});

const string CorsPolicy = "web";
builder.Services.AddCors(options => options.AddPolicy(CorsPolicy, policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
                 ?? ["http://localhost:5173"])
    .AllowAnyHeader()
    .AllowAnyMethod()
    // Tarayici korelasyon kimligini okuyabilsin; hata bildiriminde kullanilir.
    .WithExposedHeaders(CorrelationIdMiddleware.HeaderName)));

var app = builder.Build();

// Sira onemli: korelasyon kimligi once uretilir ki istek tamamlanma logu ve
// hata cevabi da ayni kimligi tasisin.
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = LoggingSetup.EnrichFromRequest;
    options.GetLevel = LoggingSetup.LevelForRequest;
});
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseCors(CorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Gloria Prim Motoru v1"));

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CommissionDbContext>();
    await DbSeeder.SeedAsync(db, ResolvePersonnelSeedPath(app));
}

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).ExcludeFromDescription();

// Seed dosyasi gelistirmede repo kokunde, konteynerde uygulama klasorunde bulunur.
static string? ResolvePersonnelSeedPath(WebApplication app)
{
    var configured = app.Configuration["Seed:PersonnelCsvPath"];
    if (!string.IsNullOrWhiteSpace(configured) && File.Exists(configured)) return configured;

    var root = app.Environment.ContentRootPath;

    string[] candidates =
    [
        Path.Combine(root, "sample-data", "personel.csv"),
        Path.Combine(root, "..", "..", "sample-data", "personel.csv"),
        Path.Combine(root, "..", "..", "..", "sample-data", "personel.csv")
    ];

    return candidates.Select(Path.GetFullPath).FirstOrDefault(File.Exists);
}

app.Run();

/// <summary>Entegrasyon testlerinin WebApplicationFactory ile baglanabilmesi icin.</summary>
public partial class Program;

using Gloria.Commission.Application.Abstractions;
using Gloria.Commission.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Gloria.Commission.Infrastructure.Persistence;

/// <summary>
/// Islem sinirinin EF Core karsiligi. Denetim kaydi ve donem kilidi
/// SaveChanges icindeki interceptor'larda calistigi icin bu cagri
/// ayni zamanda o garantilerin de tetiklendigi noktadir.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly CommissionDbContext _db;

    public UnitOfWork(CommissionDbContext db) => _db = db;

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            return await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex is not null)
        {
            // Ayni kaydi es zamanli guncelleyen iki istek gercek bir cakismadir,
            // beklenmeyen hata degil. EF istisnasi burada duruyor; ust katmanlar
            // veri erisim teknolojisini bilmiyor.
            throw new DomainException("CONCURRENT_UPDATE",
                "Bu kayit su anda baska bir islem tarafindan guncelleniyor. Lutfen tekrar deneyin.");
        }
    }
}

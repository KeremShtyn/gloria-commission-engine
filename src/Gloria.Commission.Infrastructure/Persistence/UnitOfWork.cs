using Gloria.Commission.Application.Abstractions;

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

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}

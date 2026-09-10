namespace Gloria.Commission.Application.Abstractions;

/// <summary>
/// Islem siniri. Repository'ler degisiklikleri isaretler, kalicilastirmayi servis
/// bu arayuz uzerinden yapar — boylece bir servis metodu birden fazla tabloya
/// tek islemde yazabilir (orn. aktarim: parti + satislar + hatali satirlar).
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

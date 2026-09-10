namespace Gloria.Commission.Application.Abstractions;

/// <summary>
/// Islem siniri. Repository'ler degisiklikleri isaretler, kalicilastirmayi servis
/// bu arayuz uzerinden yapar — boylece bir servis metodu birden fazla tabloya
/// tek islemde yazabilir (orn. aktarim: parti + satislar + hatali satirlar).
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>
    /// Birden fazla <see cref="SaveChangesAsync"/> gerektiren islemi tek transaction'a alir.
    /// Aktarim boyle: parti kaydi, ham satirlar, satislar ve iade eslesmeleri ayri
    /// adimlarda yazilir. Ortada bir hata cikarsa hicbiri kalmamali — aksi halde
    /// aktarim gecmisinde sayilari sifir, yarim yazilmis bir parti kalir.
    /// </summary>
    Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation, CancellationToken ct = default);
}

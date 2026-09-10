namespace Gloria.Commission.Domain.Enums;

public enum StagingRowStatus
{
    /// <summary>Alindi, henuz islenmedi.</summary>
    Pending = 1,

    /// <summary>Ayristirildi ve satis kaydina donustu.</summary>
    Processed = 2,

    /// <summary>Ayristirilamadi; gerekcesi import_errors tablosunda.</summary>
    Failed = 3,

    /// <summary>Ayristirildi ama ayni belge zaten kayitli oldugu icin yazilmadi.</summary>
    Duplicate = 4
}

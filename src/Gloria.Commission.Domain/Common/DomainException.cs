namespace Gloria.Commission.Domain.Common;

/// <summary>İş kuralı ihlali. API katmanında 422 Unprocessable Entity'ye çevrilir.</summary>
public class DomainException : Exception
{
    public string Code { get; }

    public DomainException(string code, string message) : base(message) => Code = code;
}

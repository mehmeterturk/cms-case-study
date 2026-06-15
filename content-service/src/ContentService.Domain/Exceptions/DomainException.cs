namespace ContentService.Domain.Exceptions;

/// <summary>
/// Bir domain (iş) kuralı ihlal edildiğinde fırlatılır; örneğin geçersiz bir
/// durum geçişi. API katmanında 409 Conflict'e çevrilir.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}

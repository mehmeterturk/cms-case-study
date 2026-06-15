namespace ContentService.Application.Common.Exceptions;

/// <summary>
/// Bağımlı bir servise (ör. User Service) erişilemediğinde veya beklenmeyen yanıt
/// alındığında fırlatılır. API katmanında 502 Bad Gateway'e çevrilir.
/// </summary>
public class UpstreamServiceException : Exception
{
    public UpstreamServiceException(string message, Exception? inner = null)
        : base(message, inner)
    {
    }
}

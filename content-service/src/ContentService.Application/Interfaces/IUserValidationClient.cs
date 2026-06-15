namespace ContentService.Application.Interfaces;

/// <summary>
/// User Service'e yapılan servisler arası çağrının soyutlaması. İçerik oluşturulurken
/// ilgili kullanıcının varlığını doğrulamak için kullanılır. Implementasyonu
/// Infrastructure katmanında HttpClient + resilience (Polly) ile yapılır.
/// </summary>
public interface IUserValidationClient
{
    /// <summary>
    /// Verilen kullanıcının User Service'te var olup olmadığını döndürür.
    /// Servise erişilemezse <see cref="Common.Exceptions.UpstreamServiceException"/> fırlatır.
    /// </summary>
    Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default);
}

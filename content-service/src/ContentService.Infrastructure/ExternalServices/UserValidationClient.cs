using System.Net;
using ContentService.Application.Common.Exceptions;
using ContentService.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;

namespace ContentService.Infrastructure.ExternalServices;

/// <summary>
/// User Service'e GET /users/{id} çağrısı yaparak kullanıcı varlığını doğrular.
/// Dayanıklılık (retry / timeout / circuit breaker) DI'da yapılandırılan
/// standart resilience handler (Polly) tarafından sağlanır. Tüm retry'lar
/// tükenirse <see cref="UpstreamServiceException"/> fırlatılır (API'de 502).
/// </summary>
public class UserValidationClient : IUserValidationClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UserValidationClient> _logger;

    public UserValidationClient(HttpClient httpClient, ILogger<UserValidationClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync($"users/{userId}", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }

            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (BrokenCircuitException ex)
        {
            // Circuit breaker açık: User Service'e hiç çağrı yapılmadan hızlı reddedildi.
            _logger.LogWarning(ex, "User Service devre kesicisi açık; çağrı yapılmadı (userId: {UserId})", userId);
            throw new UpstreamServiceException("Kullanıcı doğrulama servisi geçici olarak devre dışı (circuit breaker açık).", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "User Service'e erişilemedi (userId: {UserId})", userId);
            throw new UpstreamServiceException("Kullanıcı doğrulama servisine şu anda erişilemiyor.", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            // Resilience handler'ın timeout'u TaskCanceledException olarak yüzeye çıkar.
            _logger.LogError(ex, "User Service çağrısı zaman aşımına uğradı (userId: {UserId})", userId);
            throw new UpstreamServiceException("Kullanıcı doğrulama servisi zaman aşımına uğradı.", ex);
        }
    }
}

using ContentService.Application.Interfaces;
using ContentService.Infrastructure.ExternalServices;
using ContentService.Infrastructure.Persistence;
using ContentService.Infrastructure.Repositories;
using ContentService.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ContentService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("'Default' bağlantı dizesi bulunamadı.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null)));

        services.AddScoped<IContentRepository, ContentRepository>();
        services.AddScoped<IMediaAttachmentRepository, MediaAttachmentRepository>();

        // Medya depolama soketi: yapılandırmaya göre Local / S3 / AzureBlob.
        services.AddFileStorage(configuration);

        var userServiceBaseUrl = configuration["Services:UserService:BaseUrl"]
            ?? throw new InvalidOperationException("'Services:UserService:BaseUrl' yapılandırması bulunamadı.");

        // Typed HttpClient + standart resilience handler (Polly v8 tabanlı):
        // retry (exponential backoff) + timeout + circuit breaker.
        // Not: HttpClient.Timeout ayarlanmaz; zaman aşımı yönetimi resilience handler'a
        // bırakılır (aksi halde ikisi çakışır).
        services.AddHttpClient<IUserValidationClient, UserValidationClient>(client =>
        {
            client.BaseAddress = new Uri(userServiceBaseUrl);
        })
        .AddStandardResilienceHandler(options =>
        {
            // Tek bir denemenin üst sınırı.
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(5);
            // Tüm operasyon (retry'lar dahil) için üst sınır.
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(20);

            // Retry: 2 tekrar, kısa backoff (demo'da görünür ama hızlı).
            options.Retry.MaxRetryAttempts = 2;
            options.Retry.Delay = TimeSpan.FromMilliseconds(500);

            // Circuit breaker: düşük trafikli demo/case'de gözlemlenebilir eşikler.
            // (Default min throughput 100/30sn olduğundan demo'da hiç tetiklenmezdi.)
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(15);
            options.CircuitBreaker.MinimumThroughput = 4;   // en az 4 çağrı sonrası değerlendir
            options.CircuitBreaker.FailureRatio = 0.5;       // %50 hata oranında devreyi aç
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15); // 15 sn açık kal
        });

        return services;
    }
}

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
        services.AddHttpClient<IUserValidationClient, UserValidationClient>(client =>
        {
            client.BaseAddress = new Uri(userServiceBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
        })
        .AddStandardResilienceHandler();

        return services;
    }
}

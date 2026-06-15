using System.Net;
using ContentService.Application.Common.Exceptions;
using ContentService.Infrastructure.ExternalServices;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ContentService.Tests;

public class UserValidationClientTests
{
    private static UserValidationClient CreateClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://user-service/") };
        return new UserValidationClient(httpClient, NullLogger<UserValidationClient>.Instance);
    }

    [Fact]
    public async Task UserExistsAsync_200Ok_TrueDoner()
    {
        var client = CreateClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));

        Assert.True(await client.UserExistsAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task UserExistsAsync_404NotFound_FalseDoner()
    {
        var client = CreateClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)));

        Assert.False(await client.UserExistsAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task UserExistsAsync_AgHatasi_UpstreamServiceExceptionFirlatir()
    {
        var client = CreateClient(new StubHandler(_ => throw new HttpRequestException("bağlantı reddedildi")));

        await Assert.ThrowsAsync<UpstreamServiceException>(() => client.UserExistsAsync(Guid.NewGuid()));
    }

    /// <summary>İstek başına önceden tanımlı yanıt üreten basit test handler'ı.</summary>
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) => _responder = responder;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_responder(request));
    }
}

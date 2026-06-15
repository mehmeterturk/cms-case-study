using System.Net;
using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using ContentService.Infrastructure.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ContentService.Tests;

public class S3FileStorageTests
{
    private readonly Mock<IAmazonS3> _s3 = new();
    private readonly S3FileStorage _sut;

    public S3FileStorageTests()
    {
        _sut = new S3FileStorage(_s3.Object, "test-bucket", NullLogger<S3FileStorage>.Instance);
    }

    [Fact]
    public void ProviderName_S3Doner()
    {
        Assert.Equal("S3", _sut.ProviderName);
    }

    [Fact]
    public async Task SaveAsync_DogruBucketVeAnahtarlaPutObjectCagirir()
    {
        _s3.Setup(s => s.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PutObjectResponse());

        await _sut.SaveAsync("icerik/dosya.png", new MemoryStream(Encoding.UTF8.GetBytes("veri")), "image/png");

        _s3.Verify(s => s.PutObjectAsync(
            It.Is<PutObjectRequest>(r => r.BucketName == "test-bucket"
                                         && r.Key == "icerik/dosya.png"
                                         && r.ContentType == "image/png"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAsync_S3AkisiniDoner()
    {
        var payload = new MemoryStream(Encoding.UTF8.GetBytes("merhaba"));
        _s3.Setup(s => s.GetObjectAsync("test-bucket", "anahtar", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetObjectResponse { ResponseStream = payload });

        var stream = await _sut.GetAsync("anahtar");

        using var reader = new StreamReader(stream);
        Assert.Equal("merhaba", await reader.ReadToEndAsync());
    }

    [Fact]
    public async Task DeleteAsync_DeleteObjectCagirir()
    {
        _s3.Setup(s => s.DeleteObjectAsync("test-bucket", "anahtar", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteObjectResponse { HttpStatusCode = HttpStatusCode.NoContent });

        await _sut.DeleteAsync("anahtar");

        _s3.Verify(s => s.DeleteObjectAsync("test-bucket", "anahtar", It.IsAny<CancellationToken>()), Times.Once);
    }
}

using System.Text;
using ContentService.Application.Common.Exceptions;
using ContentService.Application.Interfaces;
using ContentService.Application.Models;
using ContentService.Application.Services;
using ContentService.Domain.Entities;
using Moq;
using Xunit;

namespace ContentService.Tests;

public class MediaAppServiceTests
{
    private readonly Mock<IContentRepository> _contentRepository = new();
    private readonly Mock<IMediaAttachmentRepository> _mediaRepository = new();
    private readonly Mock<IFileStorage> _storage = new();
    private readonly MediaAppService _sut;

    public MediaAppServiceTests()
    {
        _sut = new MediaAppService(_contentRepository.Object, _mediaRepository.Object, _storage.Object);
    }

    private static FileUpload SampleFile(string name = "resim.png", string type = "image/png", string body = "veri")
    {
        var bytes = Encoding.UTF8.GetBytes(body);
        return new FileUpload(new MemoryStream(bytes), name, type, bytes.Length);
    }

    [Fact]
    public async Task UploadAsync_IcerikVar_DepoyaYazarVeKaydeder()
    {
        var contentId = Guid.NewGuid();
        _contentRepository.Setup(r => r.GetByIdAsync(contentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Content { Title = "T", Body = "B", UserId = Guid.NewGuid() });

        var result = await _sut.UploadAsync(contentId, SampleFile());

        Assert.Equal("resim.png", result.FileName);
        Assert.Equal(contentId, result.ContentId);
        _storage.Verify(s => s.SaveAsync(It.IsAny<string>(), It.IsAny<Stream>(), "image/png", It.IsAny<CancellationToken>()), Times.Once);
        _mediaRepository.Verify(r => r.AddAsync(It.IsAny<MediaAttachment>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadAsync_IcerikYok_NotFoundExceptionFirlatirVeDepoyaYazmaz()
    {
        _contentRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Content?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.UploadAsync(Guid.NewGuid(), SampleFile()));
        _storage.Verify(s => s.SaveAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UploadAsync_BosDosya_ValidationExceptionFirlatir()
    {
        var contentId = Guid.NewGuid();
        _contentRepository.Setup(r => r.GetByIdAsync(contentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Content { Title = "T", Body = "B", UserId = Guid.NewGuid() });

        var emptyFile = new FileUpload(new MemoryStream(), "bos.txt", "text/plain", 0);

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => _sut.UploadAsync(contentId, emptyFile));
        _storage.Verify(s => s.SaveAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DownloadAsync_BaskaIceriginMedyasi_NotFoundExceptionFirlatir()
    {
        var contentId = Guid.NewGuid();
        var media = new MediaAttachment { ContentId = Guid.NewGuid(), StorageKey = "k", FileName = "f", ContentType = "x" };
        _mediaRepository.Setup(r => r.GetByIdAsync(media.Id, It.IsAny<CancellationToken>())).ReturnsAsync(media);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DownloadAsync(contentId, media.Id));
    }

    [Fact]
    public async Task DeleteAsync_MedyaVar_DepodanVeKayittanSiler()
    {
        var contentId = Guid.NewGuid();
        var media = new MediaAttachment { ContentId = contentId, StorageKey = "anahtar", FileName = "f", ContentType = "x" };
        _mediaRepository.Setup(r => r.GetByIdAsync(media.Id, It.IsAny<CancellationToken>())).ReturnsAsync(media);

        await _sut.DeleteAsync(contentId, media.Id);

        _storage.Verify(s => s.DeleteAsync("anahtar", It.IsAny<CancellationToken>()), Times.Once);
        _mediaRepository.Verify(r => r.DeleteAsync(media, It.IsAny<CancellationToken>()), Times.Once);
    }
}

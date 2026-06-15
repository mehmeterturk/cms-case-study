using ContentService.Application.Common.Exceptions;
using ContentService.Application.DTOs;
using ContentService.Application.Interfaces;
using ContentService.Application.Models;
using ContentService.Application.Services;
using ContentService.Application.Validators;
using ContentService.Domain.Entities;
using ContentService.Domain.Enums;
using ContentService.Domain.Exceptions;
using FluentValidation;
using Moq;
using Xunit;

namespace ContentService.Tests;

public class ContentAppServiceTests
{
    private static readonly IReadOnlyList<FileUpload> NoFiles = Array.Empty<FileUpload>();
    private readonly Mock<IContentRepository> _repository = new();
    private readonly Mock<IUserValidationClient> _userClient = new();
    private readonly Mock<IMediaAttachmentRepository> _mediaRepository = new();
    private readonly Mock<IFileStorage> _storage = new();
    private readonly ContentAppService _sut;

    public ContentAppServiceTests()
    {
        // Varsayılan: içeriğin medyası yok (silme akışı için).
        _mediaRepository.Setup(r => r.GetByContentIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<MediaAttachment>());

        _sut = new ContentAppService(
            _repository.Object,
            _userClient.Object,
            _mediaRepository.Object,
            _storage.Object,
            new CreateContentRequestValidator(),
            new UpdateContentRequestValidator());
    }

    [Fact]
    public async Task CreateAsync_KullaniciVar_IcerikOlusturur()
    {
        var userId = Guid.NewGuid();
        var request = new CreateContentRequest("Başlık", "Gövde metni", userId);
        _userClient.Setup(c => c.UserExistsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.CreateAsync(request, NoFiles);

        Assert.Equal("Başlık", result.Title);
        Assert.Equal(userId, result.UserId);
        Assert.Equal("baslik", result.Slug);                 // Türkçe-duyarlı, başlıktan üretilir
        Assert.Equal(ContentStatus.Draft.ToString(), result.Status); // yeni içerik taslak başlar
        Assert.Null(result.PublishedAt);
        _repository.Verify(r => r.AddAsync(It.IsAny<Content>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_SlugCakisirsa_SonekEkleyerekTekillestirir()
    {
        var userId = Guid.NewGuid();
        var request = new CreateContentRequest("Merhaba Dünya", "Gövde", userId);
        _userClient.Setup(c => c.UserExistsAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        // "merhaba-dunya" zaten var, "merhaba-dunya-2" boş.
        _repository.Setup(r => r.ExistsBySlugAsync("merhaba-dunya", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _repository.Setup(r => r.ExistsBySlugAsync("merhaba-dunya-2", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await _sut.CreateAsync(request, NoFiles);

        Assert.Equal("merhaba-dunya-2", result.Slug);
    }

    [Fact]
    public async Task CreateAsync_KullaniciYok_ValidationExceptionFirlatirVeKaydetmez()
    {
        var userId = Guid.NewGuid();
        var request = new CreateContentRequest("Başlık", "Gövde metni", userId);
        _userClient.Setup(c => c.UserExistsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await Assert.ThrowsAsync<ValidationException>(() => _sut.CreateAsync(request, NoFiles));
        _repository.Verify(r => r.AddAsync(It.IsAny<Content>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_UserServiceErisilemez_UpstreamExceptionYayilir()
    {
        var userId = Guid.NewGuid();
        var request = new CreateContentRequest("Başlık", "Gövde metni", userId);
        _userClient.Setup(c => c.UserExistsAsync(userId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UpstreamServiceException("User Service erişilemiyor."));

        await Assert.ThrowsAsync<UpstreamServiceException>(() => _sut.CreateAsync(request, NoFiles));
        _repository.Verify(r => r.AddAsync(It.IsAny<Content>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("", "gövde")]
    [InlineData("başlık", "")]
    public async Task CreateAsync_GecersizGirdi_ValidationExceptionVeUserServiceCagrilmaz(string title, string body)
    {
        var request = new CreateContentRequest(title, body, Guid.NewGuid());

        await Assert.ThrowsAsync<ValidationException>(() => _sut.CreateAsync(request, NoFiles));
        _userClient.Verify(c => c.UserExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_BosUserId_ValidationExceptionFirlatir()
    {
        var request = new CreateContentRequest("Başlık", "Gövde", Guid.Empty);

        await Assert.ThrowsAsync<ValidationException>(() => _sut.CreateAsync(request, NoFiles));
        _userClient.Verify(c => c.UserExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_IcerikYok_NotFoundExceptionFirlatir()
    {
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Content?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetByIdAsync_IcerikVar_DtoDoner()
    {
        var content = new Content { Title = "T", Body = "B", UserId = Guid.NewGuid() };
        _repository.Setup(r => r.GetByIdAsync(content.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(content);

        var result = await _sut.GetByIdAsync(content.Id);

        Assert.Equal(content.Id, result.Id);
        Assert.Equal("T", result.Title);
    }

    [Fact]
    public async Task GetAllAsync_TumIcerikleriEsler()
    {
        var items = new List<Content>
        {
            new() { Title = "A", Body = "x", UserId = Guid.NewGuid() },
            new() { Title = "B", Body = "y", UserId = Guid.NewGuid() }
        };
        _repository.Setup(r => r.GetAllAsync(It.IsAny<ContentStatus?>(), It.IsAny<CancellationToken>())).ReturnsAsync(items);

        var result = await _sut.GetAllAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task UpdateAsync_IcerikYok_NotFoundExceptionFirlatir()
    {
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Content?)null);

        var request = new UpdateContentRequest("Yeni", "Yeni gövde");
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.UpdateAsync(Guid.NewGuid(), request, NoFiles));
    }

    [Fact]
    public async Task UpdateAsync_IcerikVar_Gunceller()
    {
        var content = new Content { Title = "Eski", Body = "Eski gövde", UserId = Guid.NewGuid() };
        _repository.Setup(r => r.GetByIdAsync(content.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(content);

        var request = new UpdateContentRequest("Yeni", "Yeni gövde");
        var result = await _sut.UpdateAsync(content.Id, request, NoFiles);

        Assert.Equal("Yeni", result.Title);
        Assert.Equal("Yeni gövde", result.Body);
        Assert.NotNull(result.UpdatedAt);
        _repository.Verify(r => r.UpdateAsync(content, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_IcerikYok_NotFoundExceptionFirlatir()
    {
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Content?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteAsync_IcerikVar_Siler()
    {
        var content = new Content { Title = "T", Body = "B", UserId = Guid.NewGuid() };
        _repository.Setup(r => r.GetByIdAsync(content.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(content);

        await _sut.DeleteAsync(content.Id);

        _repository.Verify(r => r.DeleteAsync(content, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_TaslakIcerik_YayinaAlir()
    {
        var content = new Content { Title = "T", Body = "B", UserId = Guid.NewGuid(), Status = ContentStatus.Draft };
        _repository.Setup(r => r.GetByIdAsync(content.Id, It.IsAny<CancellationToken>())).ReturnsAsync(content);

        var result = await _sut.PublishAsync(content.Id);

        Assert.Equal(ContentStatus.Published.ToString(), result.Status);
        Assert.NotNull(result.PublishedAt);
        _repository.Verify(r => r.UpdateAsync(content, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_ZatenYayinda_DomainExceptionFirlatir()
    {
        var content = new Content { Title = "T", Body = "B", UserId = Guid.NewGuid(), Status = ContentStatus.Published };
        _repository.Setup(r => r.GetByIdAsync(content.Id, It.IsAny<CancellationToken>())).ReturnsAsync(content);

        await Assert.ThrowsAsync<DomainException>(() => _sut.PublishAsync(content.Id));
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Content>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PublishAsync_IcerikYok_NotFoundExceptionFirlatir()
    {
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Content?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.PublishAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ArchiveAsync_YayindakiIcerik_Arsivler()
    {
        var content = new Content { Title = "T", Body = "B", UserId = Guid.NewGuid(), Status = ContentStatus.Published };
        _repository.Setup(r => r.GetByIdAsync(content.Id, It.IsAny<CancellationToken>())).ReturnsAsync(content);

        var result = await _sut.ArchiveAsync(content.Id);

        Assert.Equal(ContentStatus.Archived.ToString(), result.Status);
    }

    [Fact]
    public async Task ArchiveAsync_ZatenArsivli_DomainExceptionFirlatir()
    {
        var content = new Content { Title = "T", Body = "B", UserId = Guid.NewGuid(), Status = ContentStatus.Archived };
        _repository.Setup(r => r.GetByIdAsync(content.Id, It.IsAny<CancellationToken>())).ReturnsAsync(content);

        await Assert.ThrowsAsync<DomainException>(() => _sut.ArchiveAsync(content.Id));
    }

    [Fact]
    public async Task GetBySlugAsync_Yok_NotFoundExceptionFirlatir()
    {
        _repository.Setup(r => r.GetBySlugAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Content?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetBySlugAsync("yok"));
    }

    [Fact]
    public async Task GetAllAsync_StatusFiltresi_RepositoryeIletilir()
    {
        _repository.Setup(r => r.GetAllAsync(ContentStatus.Published, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Content>());

        await _sut.GetAllAsync(ContentStatus.Published);

        _repository.Verify(r => r.GetAllAsync(ContentStatus.Published, It.IsAny<CancellationToken>()), Times.Once);
    }

    // --- Medya (içerik operasyonlarına katlanmış) ---

    private static System.Collections.Generic.List<FileUpload> OneFile() =>
        new() { new FileUpload(new MemoryStream(new byte[] { 1, 2, 3 }), "resim.png", "image/png", 3) };

    [Fact]
    public async Task CreateAsync_DosyaIle_MedyaEklerVeYanittaDoner()
    {
        var userId = Guid.NewGuid();
        var request = new CreateContentRequest("Başlık", "Gövde", userId);
        _userClient.Setup(c => c.UserExistsAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        // Yanıt, medya listesini DB'den otoritatif olarak yeniden okur.
        _mediaRepository.Setup(r => r.GetByContentIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new MediaAttachment { FileName = "resim.png", ContentType = "image/png" } });

        var result = await _sut.CreateAsync(request, OneFile());

        Assert.Single(result.Media);
        Assert.Equal("resim.png", result.Media[0].FileName);
        _storage.Verify(s => s.SaveAsync(It.IsAny<string>(), It.IsAny<Stream>(), "image/png", It.IsAny<CancellationToken>()), Times.Once);
        _mediaRepository.Verify(r => r.AddAsync(It.IsAny<MediaAttachment>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_BosDosya_ValidationExceptionFirlatir()
    {
        var userId = Guid.NewGuid();
        var request = new CreateContentRequest("Başlık", "Gövde", userId);
        _userClient.Setup(c => c.UserExistsAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var emptyFile = new System.Collections.Generic.List<FileUpload>
        {
            new(new MemoryStream(), "bos.txt", "text/plain", 0)
        };

        await Assert.ThrowsAsync<ValidationException>(() => _sut.CreateAsync(request, emptyFile));
        _storage.Verify(s => s.SaveAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DownloadMediaAsync_BaskaIceriginMedyasi_NotFoundExceptionFirlatir()
    {
        var contentId = Guid.NewGuid();
        var media = new MediaAttachment { ContentId = Guid.NewGuid(), StorageKey = "k", FileName = "f", ContentType = "x" };
        _mediaRepository.Setup(r => r.GetByIdAsync(media.Id, It.IsAny<CancellationToken>())).ReturnsAsync(media);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DownloadMediaAsync(contentId, media.Id));
    }

    [Fact]
    public async Task DeleteMediaAsync_MedyaVar_DepodanVeKayittanSiler()
    {
        var contentId = Guid.NewGuid();
        var media = new MediaAttachment { ContentId = contentId, StorageKey = "anahtar", FileName = "f", ContentType = "x" };
        _mediaRepository.Setup(r => r.GetByIdAsync(media.Id, It.IsAny<CancellationToken>())).ReturnsAsync(media);

        await _sut.DeleteMediaAsync(contentId, media.Id);

        _storage.Verify(s => s.DeleteAsync("anahtar", It.IsAny<CancellationToken>()), Times.Once);
        _mediaRepository.Verify(r => r.DeleteAsync(media, It.IsAny<CancellationToken>()), Times.Once);
    }
}

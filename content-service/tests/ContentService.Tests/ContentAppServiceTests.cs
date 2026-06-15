using ContentService.Application.Common.Exceptions;
using ContentService.Application.DTOs;
using ContentService.Application.Interfaces;
using ContentService.Application.Services;
using ContentService.Application.Validators;
using ContentService.Domain.Entities;
using FluentValidation;
using Moq;
using Xunit;

namespace ContentService.Tests;

public class ContentAppServiceTests
{
    private readonly Mock<IContentRepository> _repository = new();
    private readonly Mock<IUserValidationClient> _userClient = new();
    private readonly ContentAppService _sut;

    public ContentAppServiceTests()
    {
        _sut = new ContentAppService(
            _repository.Object,
            _userClient.Object,
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

        var result = await _sut.CreateAsync(request);

        Assert.Equal("Başlık", result.Title);
        Assert.Equal(userId, result.UserId);
        _repository.Verify(r => r.AddAsync(It.IsAny<Content>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_KullaniciYok_ValidationExceptionFirlatirVeKaydetmez()
    {
        var userId = Guid.NewGuid();
        var request = new CreateContentRequest("Başlık", "Gövde metni", userId);
        _userClient.Setup(c => c.UserExistsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await Assert.ThrowsAsync<ValidationException>(() => _sut.CreateAsync(request));
        _repository.Verify(r => r.AddAsync(It.IsAny<Content>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_UserServiceErisilemez_UpstreamExceptionYayilir()
    {
        var userId = Guid.NewGuid();
        var request = new CreateContentRequest("Başlık", "Gövde metni", userId);
        _userClient.Setup(c => c.UserExistsAsync(userId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UpstreamServiceException("User Service erişilemiyor."));

        await Assert.ThrowsAsync<UpstreamServiceException>(() => _sut.CreateAsync(request));
        _repository.Verify(r => r.AddAsync(It.IsAny<Content>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("", "gövde")]
    [InlineData("başlık", "")]
    public async Task CreateAsync_GecersizGirdi_ValidationExceptionVeUserServiceCagrilmaz(string title, string body)
    {
        var request = new CreateContentRequest(title, body, Guid.NewGuid());

        await Assert.ThrowsAsync<ValidationException>(() => _sut.CreateAsync(request));
        _userClient.Verify(c => c.UserExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_BosUserId_ValidationExceptionFirlatir()
    {
        var request = new CreateContentRequest("Başlık", "Gövde", Guid.Empty);

        await Assert.ThrowsAsync<ValidationException>(() => _sut.CreateAsync(request));
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
        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(items);

        var result = await _sut.GetAllAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task UpdateAsync_IcerikYok_NotFoundExceptionFirlatir()
    {
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Content?)null);

        var request = new UpdateContentRequest("Yeni", "Yeni gövde");
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.UpdateAsync(Guid.NewGuid(), request));
    }

    [Fact]
    public async Task UpdateAsync_IcerikVar_Gunceller()
    {
        var content = new Content { Title = "Eski", Body = "Eski gövde", UserId = Guid.NewGuid() };
        _repository.Setup(r => r.GetByIdAsync(content.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(content);

        var request = new UpdateContentRequest("Yeni", "Yeni gövde");
        var result = await _sut.UpdateAsync(content.Id, request);

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
}

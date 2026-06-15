using FluentValidation;
using Moq;
using UserService.Application.Common.Exceptions;
using UserService.Application.DTOs;
using UserService.Application.Interfaces;
using UserService.Application.Services;
using UserService.Application.Validators;
using UserService.Domain.Entities;
using Xunit;

namespace UserService.Tests;

public class UserAppServiceTests
{
    private readonly Mock<IUserRepository> _repository = new();
    private readonly UserAppService _sut;

    public UserAppServiceTests()
    {
        // Varsayılan: hiçbir e-posta veritabanında yok (toplu ekleme kontrolü için).
        _repository.Setup(r => r.GetExistingEmailsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());

        _sut = new UserAppService(
            _repository.Object,
            new CreateUserRequestValidator(),
            new UpdateUserRequestValidator());
    }

    [Fact]
    public async Task CreateAsync_GecerliIstek_KullaniciOlusturur()
    {
        var request = new CreateUserRequest("Ada Lovelace", "ada@example.com");
        _repository.Setup(r => r.ExistsByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _sut.CreateAsync(request);

        Assert.Equal("Ada Lovelace", result.FullName);
        Assert.Equal("ada@example.com", result.Email);
        Assert.NotEqual(Guid.Empty, result.Id);
        _repository.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_EpostaZatenVar_ValidationExceptionFirlatir()
    {
        var request = new CreateUserRequest("Ada Lovelace", "ada@example.com");
        _repository.Setup(r => r.ExistsByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<ValidationException>(() => _sut.CreateAsync(request));
        _repository.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("", "ada@example.com")]
    [InlineData("Ada", "gecersiz-email")]
    [InlineData("Ada", "")]
    [InlineData("Ada", "kullanıcı@example.com")]   // Türkçe 'ı' local kısımda
    [InlineData("Ada", "ada@gmâil.com")]           // Türkçe/aksanlı 'â' domain'de
    [InlineData("Ada", "test@şirket.com")]         // Türkçe 'ş' domain'de
    [InlineData("Ada", "ad a@example.com")]        // boşluk içeriyor
    public async Task CreateAsync_GecersizGirdi_ValidationExceptionFirlatir(string fullName, string email)
    {
        var request = new CreateUserRequest(fullName, email);

        await Assert.ThrowsAsync<ValidationException>(() => _sut.CreateAsync(request));
        _repository.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_KullaniciYok_NotFoundExceptionFirlatir()
    {
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetByIdAsync_KullaniciVar_DtoDoner()
    {
        var user = new User { FullName = "Grace Hopper", Email = "grace@example.com" };
        _repository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _sut.GetByIdAsync(user.Id);

        Assert.Equal(user.Id, result.Id);
        Assert.Equal("Grace Hopper", result.FullName);
    }

    [Fact]
    public async Task GetAllAsync_TumKullanicilariEsler()
    {
        var users = new List<User>
        {
            new() { FullName = "A", Email = "a@example.com" },
            new() { FullName = "B", Email = "b@example.com" }
        };
        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        var result = await _sut.GetAllAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task UpdateAsync_KullaniciYok_NotFoundExceptionFirlatir()
    {
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var request = new UpdateUserRequest("Yeni Ad", "yeni@example.com");
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.UpdateAsync(Guid.NewGuid(), request));
    }

    [Fact]
    public async Task UpdateAsync_KullaniciVar_Gunceller()
    {
        var user = new User { FullName = "Eski", Email = "eski@example.com" };
        _repository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var request = new UpdateUserRequest("Yeni Ad", "yeni@example.com");
        var result = await _sut.UpdateAsync(user.Id, request);

        Assert.Equal("Yeni Ad", result.FullName);
        Assert.Equal("yeni@example.com", result.Email);
        Assert.NotNull(result.UpdatedAt);
    }

    [Fact]
    public async Task UpdateAsync_YeniEpostaBaskasindaVar_ValidationExceptionFirlatir()
    {
        var user = new User { FullName = "Eski", Email = "eski@example.com" };
        _repository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _repository.Setup(r => r.ExistsByEmailAsync("alinmis@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new UpdateUserRequest("Yeni Ad", "alinmis@example.com");

        await Assert.ThrowsAsync<ValidationException>(() => _sut.UpdateAsync(user.Id, request));
        _repository.Verify(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_EpostaAyniKalir_TekillikKontroluAtlanir()
    {
        var user = new User { FullName = "Eski", Email = "ayni@example.com" };
        _repository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Aynı e-posta ile güncelleme: ExistsByEmailAsync çağrılmamalı, başarılı olmalı.
        var request = new UpdateUserRequest("Yeni Ad", "ayni@example.com");
        var result = await _sut.UpdateAsync(user.Id, request);

        Assert.Equal("Yeni Ad", result.FullName);
        _repository.Verify(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_KullaniciYok_NotFoundExceptionFirlatir()
    {
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteAsync_KullaniciVar_Siler()
    {
        var user = new User { FullName = "Sil", Email = "sil@example.com" };
        _repository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        await _sut.DeleteAsync(user.Id);

        _repository.Verify(r => r.DeleteAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateManyAsync_GecerliListe_HepsiniOlusturur()
    {
        var requests = new List<CreateUserRequest>
        {
            new("Ada Lovelace", "ada@example.com"),
            new("Grace Hopper", "grace@example.com")
        };

        var result = await _sut.CreateManyAsync(requests);

        Assert.Equal(2, result.Count);
        _repository.Verify(r => r.AddRangeAsync(It.Is<IEnumerable<User>>(u => u.Count() == 2), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateManyAsync_BosListe_ValidationExceptionFirlatir()
    {
        await Assert.ThrowsAsync<ValidationException>(() => _sut.CreateManyAsync(new List<CreateUserRequest>()));
        _repository.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<User>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateManyAsync_BatchIciTekrarEdenEposta_ValidationExceptionFirlatir()
    {
        var requests = new List<CreateUserRequest>
        {
            new("A", "ayni@example.com"),
            new("B", "ayni@example.com")
        };

        await Assert.ThrowsAsync<ValidationException>(() => _sut.CreateManyAsync(requests));
        _repository.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<User>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateManyAsync_VeritabaninaVarOlanEposta_ValidationExceptionFirlatir()
    {
        var requests = new List<CreateUserRequest>
        {
            new("A", "var@example.com"),
            new("B", "yeni@example.com")
        };
        _repository.Setup(r => r.GetExistingEmailsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "var@example.com" });

        await Assert.ThrowsAsync<ValidationException>(() => _sut.CreateManyAsync(requests));
        _repository.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<User>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateManyAsync_GecersizGirdiVarsa_HicbiriniEklemez()
    {
        var requests = new List<CreateUserRequest>
        {
            new("Geçerli", "gecerli@example.com"),
            new("", "gecersiz-email")
        };

        await Assert.ThrowsAsync<ValidationException>(() => _sut.CreateManyAsync(requests));
        _repository.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<User>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

using ContentService.Domain.Entities;
using ContentService.Domain.Enums;
using ContentService.Domain.Exceptions;
using Xunit;

namespace ContentService.Tests;

/// <summary>Content aggregate'inin yayın yaşam döngüsü davranışları.</summary>
public class ContentEntityTests
{
    private static Content NewContent() =>
        new() { Title = "T", Body = "B", UserId = Guid.NewGuid(), Language = Language.Tr };

    [Fact]
    public void Publish_TaslakIcerik_YayinaAlirVePublishedAtSetEder()
    {
        var content = NewContent();

        content.Publish();

        Assert.Equal(ContentStatus.Published, content.Status);
        Assert.NotNull(content.PublishedAt);
        Assert.NotNull(content.UpdatedAt);
    }

    [Fact]
    public void Publish_ZatenYayinda_DomainExceptionFirlatir()
    {
        var content = NewContent();
        content.Publish();

        Assert.Throws<DomainException>(() => content.Publish());
    }

    [Fact]
    public void Publish_ArsivdenTekrarYayin_PublishedAtDegismez()
    {
        var content = NewContent();
        content.Publish();
        var ilkYayinTarihi = content.PublishedAt;
        content.Archive();

        content.Publish(); // arşivden tekrar yayına

        Assert.Equal(ContentStatus.Published, content.Status);
        Assert.Equal(ilkYayinTarihi, content.PublishedAt); // ilk yayın tarihi korunur
    }

    [Fact]
    public void Archive_TaslakIcerik_Arsivler()
    {
        var content = NewContent();

        content.Archive();

        Assert.Equal(ContentStatus.Archived, content.Status);
        Assert.Null(content.PublishedAt); // hiç yayınlanmadıysa null kalır
    }

    [Fact]
    public void Archive_YayindakiIcerik_Arsivler()
    {
        var content = NewContent();
        content.Publish();

        content.Archive();

        Assert.Equal(ContentStatus.Archived, content.Status);
    }

    [Fact]
    public void Archive_ZatenArsivli_DomainExceptionFirlatir()
    {
        var content = NewContent();
        content.Archive();

        Assert.Throws<DomainException>(() => content.Archive());
    }

    [Fact]
    public void UpdateDetails_BaslikVeGovdeyiDegistirir_UpdatedAtSetEder()
    {
        var content = NewContent();

        content.UpdateDetails("Yeni Başlık", "Yeni gövde");

        Assert.Equal("Yeni Başlık", content.Title);
        Assert.Equal("Yeni gövde", content.Body);
        Assert.NotNull(content.UpdatedAt);
    }
}

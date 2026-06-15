using ContentService.Domain.Common;
using Xunit;

namespace ContentService.Tests;

public class SlugGeneratorTests
{
    [Theory]
    [InlineData("Merhaba Dünya", "merhaba-dunya")]
    [InlineData("İçerik Yönetimi Çöp Şiş", "icerik-yonetimi-cop-sis")]
    [InlineData("  Boşluklu   Başlık  ", "bosluklu-baslik")]
    [InlineData("Özel!! Karakterler@#", "ozel-karakterler")]
    [InlineData("ĞİÖŞÜÇ", "giosuc")]
    public void Generate_TurkceVeOzelKarakterler_AsciiSlugUretir(string input, string expected)
    {
        Assert.Equal(expected, SlugGenerator.Generate(input));
    }

    [Fact]
    public void Generate_BosGirdi_BosDoner()
    {
        Assert.Equal(string.Empty, SlugGenerator.Generate("   "));
    }
}

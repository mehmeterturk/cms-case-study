using System.Text;
using ContentService.Infrastructure.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ContentService.Tests;

public class LocalFileStorageTests : IDisposable
{
    private readonly string _root;
    private readonly LocalFileStorage _sut;

    public LocalFileStorageTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "cms-media-tests", Guid.NewGuid().ToString("N"));
        var options = Options.Create(new StorageOptions { Local = new LocalStorageOptions { RootPath = _root } });
        _sut = new LocalFileStorage(options, NullLogger<LocalFileStorage>.Instance);
    }

    [Fact]
    public async Task Save_Get_Delete_TamTurDonusu()
    {
        var key = "icerik/dosya.txt";
        var payload = "merhaba dünya";

        await _sut.SaveAsync(key, new MemoryStream(Encoding.UTF8.GetBytes(payload)), "text/plain");

        using (var stream = await _sut.GetAsync(key))
        using (var reader = new StreamReader(stream))
        {
            Assert.Equal(payload, await reader.ReadToEndAsync());
        }

        await _sut.DeleteAsync(key);
        await Assert.ThrowsAsync<FileNotFoundException>(() => _sut.GetAsync(key));
    }

    [Fact]
    public async Task Save_PathTraversal_Engellenir()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.SaveAsync("../../disari.txt", new MemoryStream([1, 2, 3]), "application/octet-stream"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}

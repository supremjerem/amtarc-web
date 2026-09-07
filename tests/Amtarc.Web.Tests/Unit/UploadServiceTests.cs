using Amtarc.Web.Options;
using Amtarc.Web.Services;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace Amtarc.Web.Tests.Unit;

/// <summary>
/// What an upload is allowed to be. The declared content type and the client filename are both
/// attacker-controlled, so these cases are all about the bytes.
/// </summary>
public sealed class UploadServiceTests : IDisposable
{
    private readonly string _directory =
        Path.Combine(Path.GetTempPath(), $"amtarc-uploads-{Guid.NewGuid():N}");

    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01];
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D];
    private static readonly byte[] Gif = [.. "GIF89a"u8.ToArray(), .. new byte[] { 1, 0, 1, 0, 0, 0 }];
    private static readonly byte[] Webp = [.. "RIFF"u8.ToArray(), .. new byte[] { 0, 0, 0, 0 }, .. "WEBP"u8.ToArray()];
    private static readonly byte[] Svg = "<svg xmlns=\"http://www.w3.org/2000/svg\"></svg>"u8.ToArray();
    private static readonly byte[] Pdf = [.. "%PDF-1.7"u8.ToArray(), .. new byte[] { 10, 37, 32, 32 }];

    private UploadService CreateService(long maxBytes = 5 * 1024 * 1024) =>
        new(
            MsOptions.Create(new UploadOptions { Directory = _directory, MaxBytes = maxBytes }),
            new FakeEnvironment(),
            NullLogger<UploadService>.Instance);

    public static TheoryData<string, byte[]> AcceptedFormats() => new()
    {
        { ".jpg", Jpeg },
        { ".png", Png },
        { ".gif", Gif },
        { ".webp", Webp },
    };

    [Theory]
    [MemberData(nameof(AcceptedFormats))]
    public async Task SaveAsync_StoresTheFile_AndNamesItFromTheSniffedType(
        string extension, byte[] bytes)
    {
        var result = await CreateService().SaveAsync(new MemoryStream(bytes), bytes.Length);

        result.Succeeded.Should().BeTrue(result.Error);
        result.Url.Should().EndWith(extension);
        Directory.GetFiles(_directory).Should().ContainSingle()
            .Which.Should().EndWith(extension);
    }

    [Theory]
    [MemberData(nameof(AcceptedFormats))]
    public async Task SaveAsync_WritesTheCompleteFile_NotJustTheSniffedHeader(
        string extension, byte[] bytes)
    {
        // The service reads the first bytes to identify the format; those bytes must still end up
        // in the file, or every image would be written truncated.
        _ = extension;
        var payload = bytes.Concat(Enumerable.Repeat((byte)0x42, 500)).ToArray();

        var result = await CreateService().SaveAsync(new MemoryStream(payload), payload.Length);

        result.Succeeded.Should().BeTrue(result.Error);
        var written = await File.ReadAllBytesAsync(Directory.GetFiles(_directory).Single());
        written.Should().Equal(payload);
    }

    [Theory]
    [InlineData("svg")]
    [InlineData("pdf")]
    public async Task SaveAsync_RejectsAFormatThatIsNotAnImage(string kind)
    {
        var bytes = kind == "svg" ? Svg : Pdf;

        var result = await CreateService().SaveAsync(new MemoryStream(bytes), bytes.Length);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("Format non accepté");
        Directory.Exists(_directory).Should().BeFalse("nothing may be written before validation passes");
    }

    [Fact]
    public async Task SaveAsync_RejectsAFileThatLiesAboutBeingAPngByExtensionAlone()
    {
        // A .png name over PDF bytes: only the bytes are consulted, so this is refused.
        var result = await CreateService().SaveAsync(new MemoryStream(Pdf), Pdf.Length);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task SaveAsync_RejectsAFileOverTheSizeLimit()
    {
        var result = await CreateService(maxBytes: 1024)
            .SaveAsync(new MemoryStream(Png), length: 2048);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("taille maximale");
    }

    [Fact]
    public async Task SaveAsync_RejectsAnEmptyFile()
    {
        var result = await CreateService().SaveAsync(new MemoryStream(), length: 0);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task SaveAsync_GivesEveryUploadItsOwnName()
    {
        var service = CreateService();

        var first = await service.SaveAsync(new MemoryStream(Png), Png.Length);
        var second = await service.SaveAsync(new MemoryStream(Png), Png.Length);

        second.Url.Should().NotBe(first.Url);
        Directory.GetFiles(_directory).Should().HaveCount(2);
    }

    [Fact]
    public async Task SaveAsync_PrefixesThePublicBaseUrlWhenOneIsConfigured()
    {
        var service = new UploadService(
            MsOptions.Create(new UploadOptions
            {
                Directory = _directory,
                PublicBaseUrl = "https://amtarc.example/",
            }),
            new FakeEnvironment(),
            NullLogger<UploadService>.Instance);

        var result = await service.SaveAsync(new MemoryStream(Png), Png.Length);

        result.Url.Should().StartWith("https://amtarc.example/uploads/").And.NotContain("//uploads");
    }

    [Fact]
    public async Task SaveAsync_ReturnsASiteRelativeUrlWhenNoBaseUrlIsConfigured()
    {
        var result = await CreateService().SaveAsync(new MemoryStream(Png), Png.Length);

        result.Url.Should().StartWith("/uploads/");
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private sealed class FakeEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Testing";
        public string ApplicationName { get; set; } = "Amtarc.Web.Tests";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

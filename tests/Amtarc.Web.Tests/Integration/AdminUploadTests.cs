using System.Net;
using Amtarc.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Amtarc.Web.Tests.Integration;

/// <summary>
/// Uploading an image through the news form: the stored URL is what the item carries, and the
/// file is actually served back.
/// </summary>
[Collection(AmtarcWebCollection.Name)]
public sealed class AdminUploadTests(AmtarcWebFactory factory)
{
    private readonly AmtarcWebFactory _factory = factory;

    private static readonly byte[] Png =
        [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52];

    private static readonly byte[] Svg =
        "<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert(1)</script></svg>"u8.ToArray();

    private static Dictionary<string, string> ItemFields(string title) => new()
    {
        ["Input.Title"] = title,
        ["Input.Category"] = "Evenement",
        ["Input.Body"] = "Une actualité avec une image.",
        ["Input.Published"] = "true",
    };

    [Fact]
    public async Task Create_WithAnImage_StoresTheUrlAndServesTheFile()
    {
        var client = await LoginClient.CreateAuthenticatedClientAsync(_factory);
        var title = $"Avec image {Guid.NewGuid():N}";

        var response = await AdminForm.PostWithFileAsync(
            client, "/admin/news/create", ItemFields(title),
            fieldName: "Image", fileName: "photo.png", bytes: Png, contentType: "image/png");

        response.StatusCode.Should().Be(HttpStatusCode.Found);

        var item = await FindByTitleAsync(title);
        item.ImageUrl.Should().StartWith("/uploads/").And.EndWith(".png");

        var served = await client.GetAsync(item.ImageUrl);
        served.StatusCode.Should().Be(HttpStatusCode.OK);
        served.Content.Headers.ContentType!.MediaType.Should().Be("image/png");
        (await served.Content.ReadAsByteArrayAsync()).Should().Equal(Png);
    }

    [Fact]
    public async Task Create_NamesTheStoredFileFromTheBytes_NeverFromTheClientFilename()
    {
        var client = await LoginClient.CreateAuthenticatedClientAsync(_factory);
        var title = $"Nom hostile {Guid.NewGuid():N}";

        var response = await AdminForm.PostWithFileAsync(
            client, "/admin/news/create", ItemFields(title),
            fieldName: "Image",
            // A client filename can carry a traversal, a double extension, anything.
            fileName: "../../evil.php.png",
            bytes: Png,
            contentType: "image/png");

        response.StatusCode.Should().Be(HttpStatusCode.Found);

        var item = await FindByTitleAsync(title);
        item.ImageUrl.Should().NotContain("evil").And.NotContain("..");
        item.ImageUrl.Should().EndWith(".png");
    }

    [Fact]
    public async Task Create_RejectsAnSvg_AndSaysSoOnTheForm()
    {
        var client = await LoginClient.CreateAuthenticatedClientAsync(_factory);

        var response = await AdminForm.PostWithFileAsync(
            client, "/admin/news/create", ItemFields($"Refusé {Guid.NewGuid():N}"),
            fieldName: "Image",
            // SVG carries script, and the declared content type is the client's word, not proof.
            fileName: "logo.svg", bytes: Svg, contentType: "image/svg+xml");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Contain("Format non accepté");
    }

    [Fact]
    public async Task Create_RejectsImageBytesMislabelledAsAPng()
    {
        var client = await LoginClient.CreateAuthenticatedClientAsync(_factory);

        var response = await AdminForm.PostWithFileAsync(
            client, "/admin/news/create", ItemFields($"Menteur {Guid.NewGuid():N}"),
            fieldName: "Image", fileName: "photo.png", bytes: Svg, contentType: "image/png");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Contain("Format non accepté");
    }

    [Fact]
    public async Task Create_WithARejectedImage_KeepsTheRestOfTheFormFilledIn()
    {
        var client = await LoginClient.CreateAuthenticatedClientAsync(_factory);
        var title = $"Texte conservé {Guid.NewGuid():N}";

        var response = await AdminForm.PostWithFileAsync(
            client, "/admin/news/create", ItemFields(title),
            fieldName: "Image", fileName: "logo.svg", bytes: Svg, contentType: "image/svg+xml");

        (await response.Content.ReadAsStringAsync()).Should().Contain(title);
    }

    [Fact]
    public async Task UploadsPath_DoesNotServeAFileTypeTheValidatorWouldHaveRejected()
    {
        var client = await LoginClient.CreateAuthenticatedClientAsync(_factory);

        var response = await client.GetAsync("/uploads/anything.svg");

        // Nothing writes an .svg there, and the content-type provider would not serve one anyway.
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<Web.Domain.News> FindByTitleAsync(string title)
    {
        await using var scope = _factory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AmtarcDbContext>();

        return await db.News.AsNoTracking().SingleAsync(n => n.Title == title);
    }
}

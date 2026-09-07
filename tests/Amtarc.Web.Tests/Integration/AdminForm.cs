using System.Net.Http.Headers;

namespace Amtarc.Web.Tests.Integration;

/// <summary>
/// Posts back-office forms the way a browser does: fetch the page, take its anti-forgery token,
/// submit. Going through the real page keeps the tests honest about the token being there.
/// </summary>
public static class AdminForm
{
    public static async Task<HttpResponseMessage> PostAsync(
        HttpClient client, string path, IDictionary<string, string> fields, string? formPath = null)
    {
        var token = await TokenFromAsync(client, formPath ?? path);
        var body = new Dictionary<string, string>(fields) { ["__RequestVerificationToken"] = token };

        return await client.PostAsync(path, new FormUrlEncodedContent(body));
    }

    /// <summary>Posts a form that carries a file, as <c>multipart/form-data</c>.</summary>
    public static async Task<HttpResponseMessage> PostWithFileAsync(
        HttpClient client,
        string path,
        IDictionary<string, string> fields,
        string fieldName,
        string fileName,
        byte[] bytes,
        string contentType,
        string? formPath = null)
    {
        var token = await TokenFromAsync(client, formPath ?? path);

        var content = new MultipartFormDataContent();
        foreach (var (key, value) in fields)
        {
            content.Add(new StringContent(value), key);
        }

        content.Add(new StringContent(token), "__RequestVerificationToken");

        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(file, fieldName, fileName);

        return await client.PostAsync(path, content);
    }

    private static async Task<string> TokenFromAsync(HttpClient client, string path) =>
        LoginClient.ExtractAntiForgeryToken(await client.GetStringAsync(path));
}

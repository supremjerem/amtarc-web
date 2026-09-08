using System.Text.Encodings.Web;
using System.Text.Unicode;
using Amtarc.Web.Data;
using Amtarc.Web.Data.Interceptors;
using Amtarc.Web.Domain;
using Amtarc.Web.Infrastructure;
using Amtarc.Web.Options;
using Amtarc.Web.Security;
using Amtarc.Web.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.WebEncoders;

// The schema carried over from Prisma uses `timestamp(3) without time zone`; keep DateTime
// mapping naive so restored production data and app writes line up. See the rewrite plan, Risk 2.
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages(options =>
{
    // Everything under /Admin needs the admin cookie; the two pages that get you one cannot.
    options.Conventions.AuthorizeFolder("/Admin", AuthenticationSetup.AdminOnlyPolicy);
    options.Conventions.AllowAnonymousToPage("/Admin/Login");
    options.Conventions.AllowAnonymousToPage("/Admin/Logout");
});

// Bound and validated up front: a deployment that left a placeholder password in place must fail
// loudly at startup, not quietly serve a back-office anyone can log into.
builder.Services.AddOptions<AdminOptions>()
    .Bind(builder.Configuration.GetSection(AdminOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<UploadOptions>()
    .Bind(builder.Configuration.GetSection(UploadOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<SecurityOptions>()
    .Bind(builder.Configuration.GetSection(SecurityOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

var security = builder.Configuration.GetSection(SecurityOptions.SectionName).Get<SecurityOptions>()
    ?? new SecurityOptions();

builder.Services.AddAdminAuthentication(builder.Environment);
builder.Services.AddAmtarcRateLimiter(security);
builder.Services.AddPublicPageOutputCache(TimeSpan.FromSeconds(security.PublicCacheSeconds));

if (security.TrustProxyHops > 0)
{
    // Behind Traefik the client IP arrives in a header; rate limiting and logs are wrong without
    // it, and trusting one hop too many lets a caller spoof it.
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.ForwardLimit = security.TrustProxyHops;
        // The proxy is a container on a private network whose address is not known ahead of time.
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

// Razor's default HTML encoder escapes every non-ASCII character to a numeric entity, so a
// site written in French would render `&#xE9;` for every "é" coming from a Razor expression.
// The page is UTF-8; let the accented characters through as themselves.
builder.Services.Configure<WebEncoderOptions>(options =>
    options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));

var connectionString = builder.Configuration.GetConnectionString("Amtarc")
    ?? throw new InvalidOperationException("Connection string 'Amtarc' is not configured.");

builder.Services.AddDbContext<AmtarcDbContext>(options =>
    options
        .UseNpgsql(connectionString, npgsql => npgsql.MapEnum<NewsCategory>("NewsCategory"))
        .AddInterceptors(new UpdatedAtInterceptor()));

builder.Services.AddSingleton<IAdminPasswordHasher, AdminPasswordHasher>();
builder.Services.AddSingleton<IUploadService, UploadService>();
builder.Services.AddSingleton<IPublicPageCache, PublicPageCache>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<INewsService, NewsService>();
builder.Services.AddScoped<ISiteContentService, SiteContentService>();
builder.Services.AddScoped<AdminSeeder>();
builder.Services.AddScoped<NewsSeeder>();

var app = builder.Build();

await MigrateAndSeedAsync(app);

// First in the pipeline: everything downstream that reads the client address or the scheme —
// the rate limiter, the secure-cookie policy, HSTS — must see the real ones.
if (security.TrustProxyHops > 0)
{
    app.UseForwardedHeaders();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseSecurityHeaders();

// Static assets are served before the rate limiter: one page load pulls a stylesheet, a script,
// fonts and images, and counting those against a per-visitor budget meant for pages would throttle
// ordinary browsing.
app.UseStaticFiles();
MapUploads(app);

app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
// After authentication, so the policy can tell an anonymous visitor (cacheable) from a signed-in
// admin, who must see their own edit immediately.
app.UseOutputCache();

app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));
app.MapRazorPages();

app.Run();

// Uploaded images live outside wwwroot — they are data, not part of the build — so they get their
// own file provider rather than being dropped into the folder that is served wholesale.
static void MapUploads(WebApplication app)
{
    var directory = app.Services.GetRequiredService<IUploadService>().ResolveDirectory();
    Directory.CreateDirectory(directory);

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(directory),
        RequestPath = "/uploads",
        // Only what the upload validator lets through is servable; anything else that ends up in
        // the directory is not handed out with a guessed content type.
        ContentTypeProvider = new FileExtensionContentTypeProvider(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [".jpg"] = "image/jpeg",
                [".png"] = "image/png",
                [".webp"] = "image/webp",
                [".gif"] = "image/gif",
            }),
        ServeUnknownFileTypes = false,
    });
}

static async Task MigrateAndSeedAsync(WebApplication app)
{
    // TODO(phase-8): wrap in a Postgres advisory lock once more than one instance can start.
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<AmtarcDbContext>();
    await db.Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<AdminSeeder>().SeedAsync();

    // Sample news outside production only — a fresh checkout should have a populated home page,
    // but the live site's news is real content.
    if (!app.Environment.IsProduction())
    {
        await scope.ServiceProvider.GetRequiredService<NewsSeeder>().SeedAsync();
    }
}

// Exposed so the test project can drive the app with WebApplicationFactory.
public partial class Program;

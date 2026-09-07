using System.Text.Encodings.Web;
using System.Text.Unicode;
using Amtarc.Web.Data;
using Amtarc.Web.Data.Interceptors;
using Amtarc.Web.Domain;
using Amtarc.Web.Security;
using Amtarc.Web.Services;
using Microsoft.EntityFrameworkCore;
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

builder.Services.AddAdminAuthentication(builder.Environment);
builder.Services.AddLoginRateLimiter(builder.Configuration);

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
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<INewsService, NewsService>();
builder.Services.AddScoped<ISiteContentService, SiteContentService>();
builder.Services.AddScoped<AdminSeeder>();
builder.Services.AddScoped<NewsSeeder>();

var app = builder.Build();

await MigrateAndSeedAsync(app);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));
app.MapRazorPages();

app.Run();

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

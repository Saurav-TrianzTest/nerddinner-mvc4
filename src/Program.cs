using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NerdDinner.ModelBinders;
using NerdDinner.Models;
using NerdDinner.Options;
using NerdDinner.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Configuration / options (replaces web.config appSettings)
// ---------------------------------------------------------------------------
builder.Services.Configure<GeoOptions>(builder.Configuration.GetSection(GeoOptions.SectionName));

// ---------------------------------------------------------------------------
// Data access (EF Core + NetTopologySuite for geography columns)
// ---------------------------------------------------------------------------
var nerdDinnerConnection = builder.Configuration.GetConnectionString("NerdDinnerContext")
    ?? throw new InvalidOperationException("Connection string 'NerdDinnerContext' not found.");
var identityConnection = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<NerdDinnerContext>(options =>
    options.UseSqlServer(nerdDinnerConnection, sql => sql.UseNetTopologySuite()));
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(identityConnection));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// ---------------------------------------------------------------------------
// Authentication (replaces Forms Authentication + SimpleMembership + OAuthWebSecurity)
// ---------------------------------------------------------------------------
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        // Preserve the legacy SimpleMembership rules: 6+ characters, no complexity requirements,
        // and no restriction on the characters allowed in a user name.
        options.Password.RequiredLength = 6;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.User.AllowedUserNameCharacters = string.Empty;
        options.User.RequireUniqueEmail = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/Login";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(2880);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// External providers are only registered when credentials are configured
// (user-secrets, environment variables or a vault - never in source control).
var authBuilder = builder.Services.AddAuthentication();

var googleSection = builder.Configuration.GetSection("Authentication:Google");
if (!string.IsNullOrEmpty(googleSection["ClientId"]) && !string.IsNullOrEmpty(googleSection["ClientSecret"]))
{
    authBuilder.AddGoogle(o =>
    {
        o.ClientId = googleSection["ClientId"]!;
        o.ClientSecret = googleSection["ClientSecret"]!;
    });
}

var microsoftSection = builder.Configuration.GetSection("Authentication:Microsoft");
if (!string.IsNullOrEmpty(microsoftSection["ClientId"]) && !string.IsNullOrEmpty(microsoftSection["ClientSecret"]))
{
    authBuilder.AddMicrosoftAccount(o =>
    {
        o.ClientId = microsoftSection["ClientId"]!;
        o.ClientSecret = microsoftSection["ClientSecret"]!;
    });
}

var facebookSection = builder.Configuration.GetSection("Authentication:Facebook");
if (!string.IsNullOrEmpty(facebookSection["AppId"]) && !string.IsNullOrEmpty(facebookSection["AppSecret"]))
{
    authBuilder.AddFacebook(o =>
    {
        o.AppId = facebookSection["AppId"]!;
        o.AppSecret = facebookSection["AppSecret"]!;
    });
}

var twitterSection = builder.Configuration.GetSection("Authentication:Twitter");
if (!string.IsNullOrEmpty(twitterSection["ConsumerKey"]) && !string.IsNullOrEmpty(twitterSection["ConsumerSecret"]))
{
    authBuilder.AddTwitter(o =>
    {
        o.ConsumerKey = twitterSection["ConsumerKey"]!;
        o.ConsumerSecret = twitterSection["ConsumerSecret"]!;
    });
}

// ---------------------------------------------------------------------------
// MVC (replaces App_Start/FilterConfig, model binder registration and Web API config)
// ---------------------------------------------------------------------------
builder.Services.AddAntiforgery(options => options.HeaderName = "RequestVerificationToken");

builder.Services
    .AddControllersWithViews(options =>
    {
        options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
        options.ModelBinderProviders.Insert(0, new PointModelBinderProvider());
    })
    .AddJsonOptions(options =>
    {
        // Keep PascalCase property names expected by NerdDinner.js
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

// ---------------------------------------------------------------------------
// Bundling (replaces System.Web.Optimization / BundleConfig)
// ---------------------------------------------------------------------------
builder.Services.AddWebOptimizer(pipeline =>
{
    pipeline.AddJavaScriptBundle("/bundles/jquery.js", "lib/jquery/jquery.min.js");
    pipeline.AddJavaScriptBundle("/bundles/jqueryval.js",
        "lib/jquery-validation/jquery.validate.min.js",
        "lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.min.js");
    pipeline.AddJavaScriptBundle("/bundles/jqueryajax.js", "lib/jquery-ajax-unobtrusive/jquery.unobtrusive-ajax.min.js");
    pipeline.AddJavaScriptBundle("/bundles/knockout.js", "lib/knockout/knockout.js");
    pipeline.AddJavaScriptBundle("/bundles/nerddinner.js", "js/NerdDinner.js", "js/site.js");
    pipeline.AddCssBundle("/bundles/site.css", "css/site.css");
});

// ---------------------------------------------------------------------------
// Application services
// ---------------------------------------------------------------------------
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IAppInfo, AppInfo>();
builder.Services.AddHttpClient<IGeolocationService, GeolocationService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
});

var app = builder.Build();

// ---------------------------------------------------------------------------
// HTTP pipeline (replaces Global.asax + HTTP modules)
// ---------------------------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseWebOptimizer();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Short links such as /123 must keep working (nrddnr.com), so this route comes first.
app.MapControllerRoute(
    name: "PrettyDetails",
    pattern: "{id:int}",
    defaults: new { controller = "Dinners", action = "Details" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Optionally apply EF Core migrations at startup (Database:MigrateOnStartup = true).
if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        await scope.ServiceProvider.GetRequiredService<NerdDinnerContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating the databases.");
        throw;
    }
}

app.Run();

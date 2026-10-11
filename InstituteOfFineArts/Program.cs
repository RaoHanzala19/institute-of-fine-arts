using InstituteOfFineArts.Data;
using InstituteOfFineArts.Domain;
using InstituteOfFineArts.Infrastructure;
using InstituteOfFineArts.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var institute = builder.Configuration.GetSection("Institute").Get<InstituteOptions>() ?? new InstituteOptions();
Ui.Currency = institute.Currency;

builder.Services.Configure<InstituteOptions>(builder.Configuration.GetSection("Institute"));
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure(3)));

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.User.RequireUniqueEmail = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders()
    .AddClaimsPrincipalFactory<AppClaimsPrincipalFactory>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.Name = "ifa.auth";
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

// Re-check the security stamp on every request so a deactivated account is signed out immediately.
builder.Services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.Zero);

builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute()));

builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = 12 * 1024 * 1024);

builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<AccountProvisioner>();
builder.Services.AddAntiforgery(o => o.HeaderName = "RequestVerificationToken");
builder.Services.AddHostedService<StatusSyncService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStatusCodePagesWithReExecute("/Home/Status", "?code={0}");

app.Use(async (ctx, next) =>
{
    var h = ctx.Response.Headers;
    h["X-Content-Type-Options"] = "nosniff";
    h["X-Frame-Options"] = "SAMEORIGIN";
    h["Referrer-Policy"] = "strict-origin-when-cross-origin";
    h["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    await next();
});

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        var path = ctx.Context.Request.Path.Value ?? "";
        if (path.StartsWith("/lib") || path.StartsWith("/uploads"))
            ctx.Context.Response.Headers.CacheControl = "public,max-age=604800";
    }
});

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", async (AppDbContext db) =>
    await db.Database.CanConnectAsync()
        ? Results.Ok(new { status = "healthy", time = DateTime.Now })
        : Results.Json(new { status = "database unavailable" }, statusCode: 503));

app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    await DbSeeder.InitializeAsync(scope.ServiceProvider);
}

app.Run();



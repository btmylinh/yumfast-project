using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using WebApp.Services;
using WebApp.Data;

var builder = WebApplication.CreateBuilder(args);

// Configure localization
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[]
    {
        new CultureInfo("vi-VN"), // Tiếng Việt
        new CultureInfo("en-US")  // English
    };

    options.DefaultRequestCulture = new RequestCulture("vi-VN");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    
    // Sử dụng cookie để lưu trữ ngôn ngữ được chọn
    options.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider());
});

// Register JSON Localization Service
builder.Services.AddSingleton<IJsonLocalizationService, JsonLocalizationService>();

// Add Entity Framework with PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add authentication (simplified for development)
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
    });

builder.Services.AddAuthorization();

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "YumFast Fast Food Management API",
        Version = "v1",
        Description = "API for managing fast food orders and products in YumFast application",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "YumFast Team",
            Email = "support@yumfast.com"
        }
    });
    
    // Include XML comments
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = System.IO.Path.Combine(System.AppContext.BaseDirectory, xmlFile);
    if (System.IO.File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    // Enable Swagger in development
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "YumFast Fast Food API v1");
        c.RoutePrefix = "api-docs"; // Swagger UI at /api-docs
    });
}

app.UseHttpsRedirection();

// Use static files
app.UseStaticFiles();

// Use localization
app.UseRequestLocalization();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Configure routes for different controllers
app.MapControllerRoute(
    name: "shop",
    pattern: "shop",
    defaults: new { controller = "Shop", action = "Index" });

app.MapControllerRoute(
    name: "product",
    pattern: "product/{id?}",
    defaults: new { controller = "Product", action = "Detail" });

app.MapControllerRoute(
    name: "user",
    pattern: "user/{action}",
    defaults: new { controller = "User" });

app.MapControllerRoute(
    name: "admin",
    pattern: "admin/{action}",
    defaults: new { controller = "Admin" });

app.MapControllerRoute(
    name: "dashboard",
    pattern: "dashboard/{action}",
    defaults: new { controller = "Dashboard" });

app.MapControllerRoute(
    name: "auth",
    pattern: "auth/{action}",
    defaults: new { controller = "Auth" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Seed database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await BannerSeeder.SeedBannersAsync(context);
}

app.Run();

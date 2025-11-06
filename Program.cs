using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Localization;
using NWebsec.AspNetCore.Middleware; 
using DotNetEnv; 
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WebApp.Data;
using WebApp.Services;

var builder = WebApplication.CreateBuilder(args);

// 0. Tải bí mật từ .env 
if (builder.Environment.IsDevelopment())
{
    // Đọc biến môi trường từ file .env nếu có
    DotNetEnv.Env.Load();

    builder.Configuration.AddEnvironmentVariables();
}
// OTP
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
//   Localization setup

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[]
    {
        new CultureInfo("vi-VN"),
        new CultureInfo("en-US")
    };

    options.DefaultRequestCulture = new RequestCulture("vi-VN");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider());
});

// Localization service

builder.Services.AddSingleton<IJsonLocalizationService, JsonLocalizationService>();


//  Database 

// DB connection 
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Cookie Auth 
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie("Cookies", options =>
{
    options.LoginPath = "/Auth/SignIn";
    options.LogoutPath = "/Auth/Logout";
    options.AccessDeniedPath = "/Auth/AccessDenied";
});

// Bảo mật
// Lấy key từ configuration (ưu tiên env vars) để không lộ key trong repo
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtKey))
{
    throw new InvalidOperationException("Jwt:Key is missing in configuration (use .env or user-secrets)");
}

var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services.AddAuthentication()
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = key
        };
    });


//   Authorization

builder.Services.AddAuthorization();

// 3. CORS: chỉ cho phép frontend ở http://localhost:3000
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy => policy.WithOrigins("http://localhost:3000")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials());
});

builder.Services.AddControllersWithViews();

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

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = System.IO.Path.Combine(System.AppContext.BaseDirectory, xmlFile);
    if (System.IO.File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Cho phép nhập JWT token trong Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "Nhập JWT token theo format: Bearer {token}",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

var app = builder.Build();
// Bảo mật cơ bản
// 1.HTTPS + HSTS: Redirect toàn bộ sang HTTPS, bật HSTS ở non-dev
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "YumFast Fast Food API v1");
        c.RoutePrefix = "api-docs";
    });
}

app.UseHttpsRedirection(); // Bắt buộc chuyển HTTP -> HTTPS
app.UseStaticFiles();
app.UseRequestLocalization();
app.UseRouting();

// 2. HTTP security headers (tương tự Helmet)
app.UseXContentTypeOptions(); 
app.UseReferrerPolicy(opts => opts.NoReferrer()); 
app.UseXXssProtection(options => options.EnabledWithBlockMode()); 
app.UseXfo(options => options.Deny()); 
app.UseCsp(options => options
    .BlockAllMixedContent()
    // Cho phép CSS từ self, Google Fonts, jsdelivr
    .StyleSources(s => s.Self().UnsafeInline().CustomSources("https://fonts.googleapis.com", "https://cdn.jsdelivr.net"))
    // Cho phép font từ self, data:, Google Fonts CDN, jsdelivr
    .FontSources(s => s.Self().CustomSources("data:", "https://fonts.gstatic.com", "https://cdn.jsdelivr.net"))
    .FormActions(s => s.Self())
    .FrameAncestors(s => s.Self())
    // Cho phép ảnh từ self, data:, Unsplash, jsdelivr
    .ImageSources(s => s.Self().CustomSources("data:", "https://images.unsplash.com", "https://cdn.jsdelivr.net"))
    // Cho phép script từ self, inline, eval, jsdelivr, Google Tag Manager, Clarity
    .ScriptSources(s => s.Self().UnsafeInline().UnsafeEval().CustomSources(
        "https://cdn.jsdelivr.net",
        "https://www.googletagmanager.com",
        "https://www.clarity.ms"
    ))
); 
// 3. Bật CORS cho frontend
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();


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


// Seed Database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await BannerSeeder.SeedBannersAsync(context);
}

app.Run();

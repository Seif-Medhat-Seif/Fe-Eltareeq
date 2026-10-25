using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RestaurantOrderingPlatform.Application;
using RestaurantOrderingPlatform.Application.Interfaces;
using RestaurantOrderingPlatform.Application.Services;
using RestaurantOrderingPlatform.Application.Settings;
using RestaurantOrderingPlatform.Infrastructure;
using RestaurantOrderingPlatform.Infrastructure.Email;
using RestaurantOrderingPlatform.Infrastructure.GoogleAuth;
using RestaurantOrderingPlatform.Infrastructure.Identity;
using RestaurantOrderingPlatform.Infrastructure.Integrations.Paymob;
using RestaurantOrderingPlatform.Infrastructure.Integrations.WhatsApp;
using RestaurantOrderingPlatform.Infrastructure.Notifications;
using RestaurantOrderingPlatform.Infrastructure.Paymob;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ---------- Serilog (structured logging, from the first line of the app) ----------
// Console sink only for now; file/Seq sinks can be added later without
// touching this call site.
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// ---------- ApplicationDbContext ----------
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// The Service layer depends on IApplicationDbContext (Application project),
// never the concrete ApplicationDbContext directly — this is the one line
// in the whole solution that bridges the interface to its implementation,
// which is exactly why Program.cs is the "Composition Root".
builder.Services.AddScoped<IApplicationDbContext>(provider =>
    provider.GetRequiredService<ApplicationDbContext>());

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IIdentityService, IdentityService>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IThirdPartyAuthService, GoogleAuthService>();
builder.Services.AddScoped<IRestaurantOwnerService, RestaurantOwnerService>();
builder.Services.AddScoped<IRestaurantService, RestaurantService>();
builder.Services.AddScoped<IMenuService, MenuService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IPaymentWebhookService, PaymentWebhookService>();
builder.Services.AddScoped<IDriverService, DriverService>();
builder.Services.AddScoped<IDriverAdminService, DriverAdminService>();
builder.Services.AddScoped<ICouponService, CouponService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IReviewAdminService, ReviewAdminService>();
builder.Services.AddScoped<IReportService, ReportService>();

// Paymob
builder.Services.Configure<PaymobSettings>(builder.Configuration.GetSection("Paymob"));
builder.Services.AddHttpClient<IPaymentGateway, PaymobGateway>();

// WhatsApp
builder.Services.Configure<WhatsAppSettings>(builder.Configuration.GetSection("WhatsApp"));
builder.Services.AddHttpClient<IWhatsAppSender, WhatsAppSender>();

// Notification queue — Singleton لأنه بيتشارك بين الـ requests والـ worker
builder.Services.AddSingleton<NotificationQueue>();
builder.Services.AddSingleton<INotificationService>(sp => sp.GetRequiredService<NotificationQueue>());
builder.Services.AddHostedService<NotificationBackgroundWorker>();

builder.Services.Configure<AuthSettings>(builder.Configuration.GetSection("AuthSettings"));

// Infrastructure DI:
builder.Services.Configure<AdminSeedSettings>(builder.Configuration.GetSection("AdminSeed"));
builder.Services.AddScoped<IdentitySeeder>();

// ---------- ASP.NET Core Identity ----------
builder.Services
    .AddIdentity<AppUser, IdentityRole<Guid>>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders(); // needed later for password-reset tokens (SMTP flow)

// ---------- JWT Bearer Authentication ----------
var jwtSection = builder.Configuration.GetSection("Jwt");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSection["Key"]!)),
            ClockSkew = TimeSpan.Zero // don't grant a grace period past the 15-minute expiry
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging(); // one structured line per request: method, path, status, duration

app.UseHttpsRedirection();

app.UseAuthentication(); // must come before UseAuthorization
app.UseAuthorization();

app.MapControllers();


// Program.cs بعد var app = builder.Build(); وقبل app.Run():
using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<IdentitySeeder>().SeedAsync();
}

app.Run();
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using backend.Data;
using backend.Models;
using backend.Services;
using backend.Configurations;
using backend.Middleware;
using backend.Repositories;
using Serilog;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.SignalR;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Authentication;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// Banner at the very top
Console.WriteLine(@"\n===========================================\n   Prime Management App V3\n   Version: 3.0\n   Enterprise-grade platform for:\n   - Agent Management & Attendance Tracking\n   - Group Performance Monitoring\n   - Role-based Operations\n   - Real-time Analytics\n===========================================\n");

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/backend-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Configure Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity removed - using custom authentication

// Configure JWT Settings
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();

// Configure JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings!.SecretKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// Register services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddHttpClient<IExternalAuthService, ExternalAuthService>(client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("PrimeManagementBackend/3.0");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    client.Timeout = TimeSpan.FromSeconds(25);
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    var systemProxy = WebRequest.DefaultWebProxy;
    if (systemProxy != null)
    {
        try { systemProxy.Credentials = CredentialCache.DefaultCredentials; } catch {}
    }

    return new SocketsHttpHandler
{
    Proxy = systemProxy,
    UseProxy = true,
    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
    PooledConnectionLifetime = TimeSpan.FromMinutes(5),
    SslOptions = new SslClientAuthenticationOptions
    {
        EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
    }
    };
});

// HttpClient for external clients endpoint (proposals)
builder.Services.AddHttpClient<IExternalClientService, ExternalClientService>(client =>
{
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Admin allow-list by phone number for system admins
var adminPhones = builder.Configuration.GetSection("AdminAllowList:Phones").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddSingleton(adminPhones);

// Register business services
builder.Services.AddScoped<IAgentService, AgentService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IAttendanceTimeframeService, AttendanceTimeframeService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
// Clients removed: do not register IClientsCollectedService or IClientService
builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<IManagerService, ManagerService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IUserService, UserService>();
// External-only mode: do not register local collected proposals storage/services

// Register repositories
builder.Services.AddScoped<IAgentRepository, AgentRepository>();
builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();
builder.Services.AddScoped<IAttendanceTimeframeRepository, AttendanceTimeframeRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
// Clients removed: do not register client repositories
builder.Services.AddScoped<IGroupRepository, GroupRepository>();
builder.Services.AddScoped<IManagerRepository, ManagerRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Configure AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// For FluentValidation in .NET 9, use AddFluentValidation() if available, otherwise ensure validators are registered manually.
// builder.Services.AddFluentValidationAutoValidation(); // Commented out: not available in .NET 9

// Add HealthChecks for DB and Redis
builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException())
    .AddRedis(builder.Configuration.GetConnectionString("Redis") ?? throw new InvalidOperationException());

// For Prometheus, ensure the prometheus-net.AspNetCore package is installed and add the correct using if needed.
// builder.Services.AddHostedService<Prometheus.MetricPusher>(); // Commented out: add package if needed
// app.UseMetricServer(); // exposes /metrics - commented out until package is added

// Add Redis distributed cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

// Update SwaggerGen registration with JWT Authentication
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Prime Management App V3",
        Version = "3.0",
        Description = @"Enterprise-grade platform for:\n- Agent Management & Attendance Tracking\n- Group Performance Monitoring\n- Role-based Operations\n- Real-time Analytics",
        Contact = new OpenApiContact
        {
            Name = "Prime Management Team",
            Email = "support@primemanagement.com"
        }
    });

    // Add JWT Authentication to Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter your token below (without 'Bearer' prefix):",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });

    // Include XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// Add SignalR
builder.Services.AddSignalR();

var app = builder.Build();
// Error handling middleware (optional, for production robustness)
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        
        var error = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        if (error != null)
        {
            var ex = error.Error;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Internal Server Error",
                message = ex.Message,
                details = app.Environment.IsDevelopment() ? ex.StackTrace : null
            });
        }
    });
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Use CORS
app.UseCors("AllowAll");

// Use Rate Limiting Middleware before authentication
app.UseMiddleware<RateLimitingMiddleware>();

// Swagger should be configured before authentication for development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Prime Management App V3");
        c.RoutePrefix = "swagger";
        c.DocumentTitle = "Prime Management API Documentation";
        c.DefaultModelsExpandDepth(2);
        c.DefaultModelExpandDepth(2);
        c.DisplayRequestDuration();
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
        
        // Custom CSS for better styling
        c.InjectStylesheet("/swagger-ui/custom.css");
        
        // Add custom JavaScript for better UX
        c.InjectJavascript("/swagger-ui/custom.js");
    });
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Apply database migrations
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();
}

// Map health checks and metrics
app.MapHealthChecks("/health");
app.MapHub<backend.Controllers.NotificationHub>("/ws");

app.Run();

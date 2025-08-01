using Microsoft.AspNetCore.Identity;
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

var builder = WebApplication.CreateBuilder(args);

// Banner at the very top
Console.WriteLine(@"\n===========================================\n   Prime Management App V3\n   Version: 3.0\n   Enterprise-grade platform for:\n   - Agent Management & Attendance Tracking\n   - Group Performance Monitoring\n   - Role-based Operations\n   - Client Collection Tracking\n   - Real-time Analytics\n===========================================\n");

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

// Configure Identity with custom User model
builder.Services.AddIdentity<User, IdentityRole<long>>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

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

// Register business services
builder.Services.AddScoped<IAgentService, AgentService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IAttendanceTimeframeService, AttendanceTimeframeService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IClientsCollectedService, ClientsCollectedService>();
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<IManagerService, ManagerService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IUserService, UserService>();

// Register repositories
builder.Services.AddScoped<IAgentRepository, AgentRepository>();
builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();
builder.Services.AddScoped<IAttendanceTimeframeRepository, AttendanceTimeframeRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<IClientsCollectedRepository, ClientsCollectedRepository>();
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

// Update SwaggerGen registration
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Prime Management App V3",
        Version = "3.0",
        Description = @"Enterprise-grade platform for:\n- Agent Management & Attendance Tracking\n- Group Performance Monitoring\n- Role-based Operations\n- Client Collection Tracking\n- Real-time Analytics"
    });
});

// Add SignalR
builder.Services.AddSignalR();

var app = builder.Build();
// Error handling middleware (optional, for production robustness)
app.UseExceptionHandler("/error");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Apply database migrations
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();
}

// Use CORS
app.UseCors("AllowAll");

// Use Rate Limiting Middleware before authentication
app.UseMiddleware<RateLimitingMiddleware>();

// Map health checks and metrics
app.MapHealthChecks("/health");
app.UseSwagger();
app.UseSwaggerUI();
app.MapHub<backend.Controllers.NotificationHub>("/ws");

app.Run();


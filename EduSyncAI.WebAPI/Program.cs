using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Sqlite;
using EduSyncAI.WebAPI.Data;
using EduSyncAI.WebAPI;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<EduSyncAI.WebAPI.Services.GeminiSummarizationService>();
builder.Services.AddSingleton<EduSyncAI.WebAPI.Services.LiveStreamService>();
builder.Services.AddSignalR();

// Configure database
var dbPath = Path.Combine(builder.Environment.ContentRootPath, "..", "Data", "edusync.db");
builder.Services.AddDbContext<EduSyncDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// Configure CORS for web app
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWebApp", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)  // Allow any origin for dev/LAN testing
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Configure JWT Authentication
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? "EduSyncAI-Super-Secret-Key-For-JWT-Authentication-Min-32-Chars";
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = "EduSyncAI",
        ValidAudience = "EduSyncAI-Web",
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(jwtSecret))
    };
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowWebApp");

// Serve uploaded files (profile photos, etc.) from Data/uploads via /uploads/...
var uploadsPath = Path.Combine(builder.Environment.ContentRootPath, "..", "Data", "uploads");
Directory.CreateDirectory(uploadsPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(Path.GetFullPath(uploadsPath)),
    RequestPath = "/uploads"
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<EduSyncAI.WebAPI.Hubs.ClassroomHub>("/hubs/classroom");

// Ensure database exists and seed test user
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<EduSyncDbContext>();
    context.Database.EnsureCreated();
    
    // Seed test user
    DatabaseSeeder.SeedTestUser(context);
}

Console.WriteLine($"✅ EduSyncAI Web API starting...");
Console.WriteLine($"📁 Database: {dbPath}");
Console.WriteLine($"🌐 Swagger UI: http://localhost:5152/swagger");
Console.WriteLine($"📡 SignalR Hub: http://localhost:5152/hubs/classroom");

app.Run();

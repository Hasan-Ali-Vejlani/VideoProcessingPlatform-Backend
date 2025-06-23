// VideoProcessingPlatform.Api/Program.cs

// --- Required Using Statements ---\
using Microsoft.EntityFrameworkCore; // For DbContext and SQL Server configuration
using VideoProcessingPlatform.Infrastructure.Data; // Your DbContext (requires reference to Infrastructure)
using VideoProcessingPlatform.Core.Interfaces; // All your interfaces (from Core project)
using VideoProcessingPlatform.Infrastructure.Repositories; // Your concrete repositories (from Infrastructure project)
using VideoProcessingPlatform.Infrastructure.Services; // Your concrete infrastructure services (e.g., JWTService, from Infrastructure project)
using VideoProcessingPlatform.Api.Services; // Your concrete API-specific services (e.g., AuthService, from Api project)
using Microsoft.AspNetCore.Authentication.JwtBearer; // For JWT authentication middleware
using Microsoft.IdentityModel.Tokens; // For JWT token validation parameters
using System.Text; // For Encoding
using Microsoft.OpenApi.Models; // For Swagger UI security definitions
using System; // For InvalidOperationException
using System.Reflection; // Required for Assembly.GetExecutingAssembly().GetName().Name

var builder = WebApplication.CreateBuilder(args);

// --- Configure Services ---\

// 1. Add Controllers
builder.Services.AddControllers();

// 2. Configure Database Context (Entity Framework Core)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    // Option A: Use SQL Server LocalDB (most common for dev, typically installed with VS)
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlServerOptionsAction: sqlOptions =>
        {
            sqlOptions.MigrationsAssembly(Assembly.GetExecutingAssembly().GetName().Name);
        });
    // Option B: Use an in-memory database for testing (good for quick local tests, but data is not persistent)
    // options.UseInMemoryDatabase("VppDbInMemory");
});

// 3. Register your services and repositories for Dependency Injection
// Authentication/Authorization related services
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IJWTService, JWTService>();
builder.Services.AddScoped<IAuthService, AuthService>(); // Your AuthService in Api project

// --- Upload Related Services and Repositories ---
builder.Services.AddScoped<IUploadMetadataRepository, UploadMetadataRepository>();
builder.Services.AddSingleton<IFileStorageService, AzureBlobStorageService>();
builder.Services.AddScoped<IUploadService, UploadService>();

// --- Encoding Profile Related Services and Repositories ---
builder.Services.AddScoped<IEncodingProfileRepository, EncodingProfileRepository>();
builder.Services.AddScoped<IFFmpegCommandBuilder, FFmpegCommandBuilder>();
builder.Services.AddScoped<IEncodingProfileService, EncodingProfileService>();

// --- New: Transcoding Job and Message Queue Related Services ---
builder.Services.AddScoped<ITranscodingJobRepository, TranscodingJobRepository>();
builder.Services.AddSingleton<IMessageQueueService, InMemoryMessageQueueService>(); // Register the in-memory queue
builder.Services.AddScoped<IVideoProcessingService, VideoProcessingService>();
// --- End New Transcoding Job and Message Queue Related Services ---


// 4. Configure JWT Authentication
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Example policy: require Admin role for specific actions
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    // Example policy: require any authenticated user
    options.AddPolicy("AuthenticatedUser", policy => policy.RequireAuthenticatedUser());
});

// 5. Configure CORS (Cross-Origin Resource Sharing)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp",
        policy =>
        {
            policy.WithOrigins("http://localhost:4200") // Allow your Angular app's URL
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Video Processing Platform API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\""
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            Array.Empty<string>()
        }
    });
});

// --- Configure the HTTP Request Pipeline ---\
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Video Processing Platform API v1");
    });
}

app.UseCors("AllowAngularApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

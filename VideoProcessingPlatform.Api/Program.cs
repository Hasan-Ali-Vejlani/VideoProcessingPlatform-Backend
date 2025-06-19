// VideoProcessingPlatform.Api/Program.cs

// --- Required Using Statements ---
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

var builder = WebApplication.CreateBuilder(args);

// --- Configure Services ---

// 1. Add Controllers
builder.Services.AddControllers();

// 2. Configure Database Context (Entity Framework Core)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    // Option A: Use SQL Server LocalDB (most common for dev, typically installed with VS)
    // "Server=(localdb)\\MSSQLLocalDB;Database=VppDb;Trusted_Connection=True;MultipleActiveResultSets=true"
    //
    // Option B: Use a full SQL Server instance installed on your PC
    // Replace 'YOUR_PC_NAME\SQLEXPRESS' with the actual server name/instance
    // You can find your SQL Server instance name by opening SQL Server Management Studio (SSMS)
    // and looking at the server name when you connect, or in SQL Server Configuration Manager.
    // Example: "Server=localhost\\SQLEXPRESS;Database=VppDb;Trusted_Connection=True;MultipleActiveResultSets=true"
    // Or if it's a default instance on localhost: "Server=.;Database=VppDb;Trusted_Connection=True;MultipleActiveResultSets=true"
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), sqlOptions => sqlOptions.MigrationsAssembly("VideoProcessingPlatform.Api"));
});

// 3. Register Services and Repositories for Dependency Injection
// Using AddScoped ensures one instance per HTTP request, suitable for DbContext and most services.
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IJWTService, JWTService>();
builder.Services.AddScoped<IAuthService, AuthService>(); // Note: AuthService lives in Api project, not Infrastructure

// 4. Configure CORS (Cross-Origin Resource Sharing)
// This is crucial to allow your Angular frontend (running on a different port/origin) to make requests to your API.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp",
        policy => policy.WithOrigins("http://localhost:4200") // Explicitly allow your Angular dev server URL
                        .AllowAnyHeader() // Allows all common HTTP headers (e.g., Content-Type, Authorization)
                        .AllowAnyMethod() // Allows all HTTP methods (GET, POST, PUT, DELETE, etc.)
                        .AllowCredentials()); // Allows sending cookies, authorization headers, etc.
});

// 5. Add JWT Authentication Scheme Configuration
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme; // Default scheme for authentication
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;   // Default scheme for challenge (e.g., 401 Unauthorized)
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,           // Validate the server that created the token
        ValidateAudience = true,         // Validate the recipient of the token is authorized
        ValidateLifetime = true,         // Validate the token's expiration date
        ValidateIssuerSigningKey = true, // Validate the token's signature key (ensures token hasn't been tampered with)

        ValidIssuer = builder.Configuration["Jwt:Issuer"],     // Set valid issuer from appsettings.json
        ValidAudience = builder.Configuration["Jwt:Audience"], // Set valid audience from appsettings.json
        // Get the signing key from appsettings.json and encode it
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT:Key not found in configuration. Please add a strong, random key to appsettings.json.")))
    };
});

// 6. Add Authorization Services
builder.Services.AddAuthorization(); // This enables the use of [Authorize] attributes on controllers/actions.

// 7. Configure Swagger/OpenAPI for API Documentation and Testing (with JWT support)
builder.Services.AddEndpointsApiExplorer(); // Required for Minimal APIs, good practice to include
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Video Processing Platform API", Version = "v1" });
    // Configure Swagger to use JWT Bearer authentication in the UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer" // The name of the authentication scheme for Swagger
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
            Array.Empty<string>() // Empty array means no specific roles are required for this security scheme
        }
    });
});

// --- Configure the HTTP Request Pipeline ---
var app = builder.Build();

// Configure the HTTP request pipeline for development environment (Swagger UI)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Video Processing Platform API v1");
    });
}

// app.UseHttpsRedirection(); // Redirects HTTP requests to HTTPS

// IMPORTANT: Order of CORS, Authentication, Authorization middleware matters!
app.UseCors("AllowAngularApp"); // CORS must be placed before UseAuthentication and UseAuthorization

app.UseAuthentication(); // Adds the authentication middleware (reads JWT token)
app.UseAuthorization(); // Adds the authorization middleware (enforces [Authorize] attributes)

app.MapControllers(); // Maps incoming requests to controller actions

app.Run(); // Starts the application
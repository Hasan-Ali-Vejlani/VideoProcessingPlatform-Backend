using Microsoft.EntityFrameworkCore; // For DbContext and SQL Server configuration
using VideoProcessingPlatform.Infrastructure.Data; // Your DbContext
using VideoProcessingPlatform.Core.Interfaces; // All your interfaces
using VideoProcessingPlatform.Infrastructure.Repositories; // Your concrete repositories
using VideoProcessingPlatform.Infrastructure.Services; // Your concrete infrastructure services (like JWTService)
using VideoProcessingPlatform.Api.Services; // Your concrete API-specific services (like AuthService)
using Microsoft.AspNetCore.Authentication.JwtBearer; // For JWT authentication middleware
using Microsoft.IdentityModel.Tokens; // For JWT token validation parameters
using System.Text; // For Encoding
using Microsoft.OpenApi.Models; // For Swagger UI security definitions

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Configure Database Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

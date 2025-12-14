// Create a builder to use settings

using System.Data;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using WorldMusicJam.Helpers;
using WorldMusicJam.Repositories;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

// Look at end points and send it to the swagger
builder.Services.AddEndpointsApiExplorer();
// User the end points sent and generate swagger data set
builder.Services.AddSwaggerGen();

// Add policies to access the API from different origins (Cross origins)
builder.Services.AddCors((options) =>
{
    options.AddPolicy("DevCors", (corsBuilder) =>
    {
        corsBuilder.WithOrigins("http://localhost:4200", "http://localhost:3000", "http://localhost:8000")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
    options.AddPolicy("ProdCors", (corsBuilder) =>
    {
        corsBuilder.WithOrigins("https://myProductionSite.com")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Add interfaces
builder.Services.AddScoped<IUserRepository, UserRepositoryEntityFramework>();

// Token
var tokenSecretKey = builder.Configuration["AppSettings:JWTSecret"] ?? throw new Exception("No JWTSecret found");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
    {
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenSecretKey)),
        ValidateIssuer = false,
        ValidateIssuerSigningKey = false,
        ValidateAudience = false,
    });

// Build
var app = builder.Build();

// Configure pipeline https if not on development
if (app.Environment.IsDevelopment())
{
    app.UseCors("DevCors");
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseCors("ProdCors");
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
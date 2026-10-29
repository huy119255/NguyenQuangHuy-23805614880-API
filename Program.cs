
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NguyenQuangHuy_23805614880_API.Data;
using NguyenQuangHuy_23805614880_API.Models;
using NguyenQuangHuy_23805614880_API.Services;
using NSwag;
using NSwag.AspNetCore;
using NSwag.Generation.Processors.Security;

var builder = WebApplication.CreateBuilder(args);

// Ket noi SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString(
            "DefaultConnection")));

// Bam mat khau tai khoan
builder.Services.AddScoped<
    IPasswordHasher<User>,
    PasswordHasher<User>>();

// Dich vu gui OTP qua Gmail
builder.Services.AddScoped<EmailService>();

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Chua cau hinh Jwt:Key.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "ChamCongNhom6",
            ValidAudience = "ChamCongNhom6Mobile",
            IssuerSigningKey = new SymmetricSecurityKey(
                Convert.FromBase64String(jwtKey))
        };
    });

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddOpenApiDocument(settings =>
{
    settings.Title = "Cham Cong Nhom 6 API";
    settings.Version = "v1";
    settings.AddSecurity("Bearer", new OpenApiSecurityScheme
    {
        Type = OpenApiSecuritySchemeType.ApiKey,
        Name = "Authorization",
        In = OpenApiSecurityApiKeyLocation.Header,
        Description = "Nhap: Bearer {JWT token}"
    });
    settings.OperationProcessors.Add(
        new AspNetCoreOperationSecurityScopeProcessor("Bearer"));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

var swaggerEnabled = app.Environment.IsDevelopment() ||
    app.Configuration.GetValue<bool>("Swagger:Enabled");
if (swaggerEnabled)
{
    app.UseOpenApi(settings => settings.Path = "/swagger/{documentName}/swagger.json");
    app.UseSwaggerUi(settings =>
    {
        settings.Path = "/swagger";
        settings.SwaggerRoutes.Add(
            new SwaggerUiRoute("v1", "/swagger/v1/swagger.json"));
    });
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Trang chu kiem tra API public
app.MapGet("/", () => Results.Ok(new
{
    message = "API Cham Cong Nhom 6",
    status = "Online"
}));

app.Run();

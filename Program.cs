using Microsoft.EntityFrameworkCore;
using MyBackend.DataBase;
using MyBackend.Interface;
using MyBackend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configuración existente
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Base de datos
builder.Services.AddDbContext<DBContext>(
    option => option.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// Servicios existentes
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// Nuevo servicio para Ably (Agregar esto)
builder.Services.AddSingleton(provider => 
{
    var apiKey = provider.GetRequiredService<IConfiguration>()["Ably:ApiKey"];
    return new AblyService(apiKey);
});

// Autenticación JWT (existente)
builder.Services.AddAuthentication(options =>
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
        ValidIssuer = builder.Configuration["JWT:Issuer"],
        ValidAudience = builder.Configuration["JWT:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["JWT:SecretKey"] ?? ""))
    };
});

// CORS (existente - verifica los orígenes)
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors",
        policy => policy
            .WithOrigins("http://localhost:5173") // Asegúrate que coincide con tu frontend
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()); // Agrega esto si usas autenticación
});

var app = builder.Build();

// Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseRouting();
app.UseCors("DevCors");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
using LdapSync.Application.DTOs;
using LdapSync.Application.Services;
using LdapSync.Domain.Interfaces;
using LdapSync.Infrastructure.Data;
using LdapSync.Infrastructure.Repositories;
using LdapSync.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "LDAP Sync API",
        Version = "v1",
        Description = "API para sincronización de usuarios y grupos desde servidores LDAP"
    });
});

// Configure MySQL Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=localhost;Database=ldapsync;User=root;Password=;";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Register Repositories
builder.Services.AddScoped<ILdapServerRepository, LdapServerRepository>();
builder.Services.AddScoped<ILdapUserRepository, LdapUserRepository>();
builder.Services.AddScoped<ILdapGroupRepository, LdapGroupRepository>();
builder.Services.AddScoped<ISyncConfigurationRepository, SyncConfigurationRepository>();
builder.Services.AddScoped<ISyncLogRepository, SyncLogRepository>();
builder.Services.AddScoped<IUserGroupMembershipRepository, UserGroupMembershipRepository>();

// Register Services
builder.Services.AddScoped<ILdapService, LdapService>();
builder.Services.AddScoped<ISyncService, SyncService>();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Ensure database is created and migrations are applied
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    try
    {
        // EF Core MigrateAsync se encarga de:
        // 1. Crear la base de datos si no existe.
        // 2. Crear la tabla __EFMigrationsHistory si no existe.
        // 3. Aplicar solo las migraciones pendientes.
        await dbContext.Database.MigrateAsync();
        
        Console.WriteLine("Base de datos verificada y migraciones aplicadas exitosamente.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error al verificar/aplicar migraciones: {ex.Message}");
        throw;
    }
}


    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "LDAP Sync API V1");
        c.RoutePrefix = string.Empty;
    });


app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

Console.WriteLine("===========================================");
Console.WriteLine("LDAP Sync API iniciada correctamente");
Console.WriteLine("Escuchando en: http://0.0.0.0:1054");
Console.WriteLine("Acceda a Swagger en: http://localhost:1054");
Console.WriteLine("===========================================");

app.Run("http://0.0.0.0:1054");

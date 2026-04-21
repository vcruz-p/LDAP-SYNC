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
    
    // Check if there are pending migrations
    try
    {
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
        var hasPendingMigrations = pendingMigrations.Any();
        
        if (hasPendingMigrations)
        {
            Console.WriteLine($"Se encontraron {pendingMigrations.Count()} migraciones pendientes: {string.Join(", ", pendingMigrations)}");
        }
        else
        {
            Console.WriteLine("No hay migraciones pendientes.");
        }
        
        // Create database if it doesn't exist
        var created = await dbContext.Database.EnsureCreatedAsync();
        
        if (created)
        {
            Console.WriteLine("Base de datos 'ldapsync' creada exitosamente.");
        }
        else
        {
            Console.WriteLine("La base de datos ya existe.");
        }

        // Apply any pending migrations
        await dbContext.Database.MigrateAsync();
        Console.WriteLine("Migraciones aplicadas exitosamente.");
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

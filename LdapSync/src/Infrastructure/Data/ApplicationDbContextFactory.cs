using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LdapSync.Infrastructure.Data;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        
        // Configura la cadena de conexión según tu entorno
        // Puedes cambiar "Server=localhost;Database=ldapsync;User=root;Password=tupassword;" 
        // por tu propia cadena de conexión
        var connectionString = "Server=localhost;Database=ldapsync;User=root;Password=;";
        
        optionsBuilder.UseMySql(
            connectionString,
            new MySqlServerVersion(new Version(8, 0, 0)),
            b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)
        );

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}

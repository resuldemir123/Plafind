using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Plafind.Data;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace Plafind
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(Directory.GetCurrentDirectory()))
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");
            var databaseProvider = configuration["Database:Provider"] ?? "MySQL";

            // Connection string'e göre veritabanı tipini belirle
            var isMySql = databaseProvider.Equals("MySQL", StringComparison.OrdinalIgnoreCase) ||
                          (!string.IsNullOrEmpty(connectionString) && 
                           (connectionString.Contains("Port=") || 
                            connectionString.Contains("User=") || 
                            connectionString.Contains("CharSet=")) &&
                           !connectionString.Contains("MSSQLLocalDB") &&
                           !connectionString.Contains("Trusted_Connection") &&
                           !connectionString.Contains("Integrated Security"));

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            if (isMySql)
            {
                var serverVersion = ServerVersion.Parse("8.0.21-mysql");
                optionsBuilder.UseMySql(connectionString, serverVersion, mySqlOptions =>
                {
                    mySqlOptions.SchemaBehavior(MySqlSchemaBehavior.Ignore);
                    mySqlOptions.MigrationsAssembly("Plafind");
                });
            }
            else
            {
                optionsBuilder.UseSqlServer(connectionString);
            }

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
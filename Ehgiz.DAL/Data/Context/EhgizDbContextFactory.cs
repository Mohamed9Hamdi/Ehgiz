using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Ehgiz.DAL.Data;

public class EhgizDbContextFactory : IDesignTimeDbContextFactory<EhgizDbContext>
{
    public EhgizDbContext CreateDbContext(string[] args)
    {
        var apiPath = ResolveApiPath();
        LoadEnvFile(Path.Combine(apiPath, ".env"));

        var configuration = new ConfigurationBuilder()
            .SetBasePath(apiPath)
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        var optionsBuilder = new DbContextOptionsBuilder<EhgizDbContext>();
        optionsBuilder.UseSqlServer(
            connectionString,
            sql => sql.MigrationsAssembly(typeof(EhgizDbContext).Assembly.GetName().Name));

        return new EhgizDbContext(optionsBuilder.Options);
    }

    private static string ResolveApiPath()
    {
        var cwd = Directory.GetCurrentDirectory();
        var candidates = new[]
        {
            cwd,
            Path.Combine(cwd, "Ehgiz.API"),
            Path.Combine(cwd, "..", "Ehgiz.API"),
            Path.Combine(cwd, "..", "..", "Ehgiz.API"),
        };

        foreach (var candidate in candidates)
        {
            var fullPath = Path.GetFullPath(candidate);
            if (File.Exists(Path.Combine(fullPath, "appsettings.json")))
                return fullPath;
        }

        throw new InvalidOperationException("Could not locate Ehgiz.API project directory.");
    }

    private static void LoadEnvFile(string path)
    {
        if (!File.Exists(path))
            return;

        foreach (var line in File.ReadAllLines(path))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
                continue;

            var separatorIndex = trimmed.IndexOf('=');
            if (separatorIndex <= 0)
                continue;

            var key = trimmed[..separatorIndex].Trim();
            var value = trimmed[(separatorIndex + 1)..].Trim();
            Environment.SetEnvironmentVariable(key, value);
        }
    }
}

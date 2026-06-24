using Ehgiz.DAL.Data;
using Microsoft.EntityFrameworkCore;

namespace Ehgiz.API.Extensions;

public static class DatabaseExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<EhgizDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(EhgizDbContext).Assembly.GetName().Name)));

        return services;
    }
}

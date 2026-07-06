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
                sql =>
                {
                    sql.MigrationsAssembly(typeof(EhgizDbContext).Assembly.GetName().Name);
                    sql.CommandTimeout(120);
                    sql.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);

                    // Several read paths (e.g. booking details) load multiple collection
                    // navigations at once; split queries avoid the row-explosion cartesian
                    // join that a single query would produce for those includes.
                    sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                }));

        return services;
    }
}

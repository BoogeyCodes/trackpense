using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Trackpense.Core.Data;
using Trackpense.Core.Services;

namespace Trackpense.Core;

public static class ServiceRegistration
{
    /// <summary>Register the database and the application services.</summary>
    public static IServiceCollection AddTrackpense(this IServiceCollection services, string connectionString)
    {
        services.AddDbContextFactory<AppDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IBudgetService, BudgetService>();
        services.AddScoped<ISummaryService, SummaryService>();
        return services;
    }
}

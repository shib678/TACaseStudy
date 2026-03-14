using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RetailPricing.Application.Common.Interfaces;
using RetailPricing.Domain.Repositories;
using RetailPricing.Infrastructure.Persistence;
using RetailPricing.Infrastructure.Persistence.Repositories;
using RetailPricing.Infrastructure.Services;

namespace RetailPricing.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                    sqlOptions.CommandTimeout(60);
                    sqlOptions.MigrationsAssembly(
                        typeof(ApplicationDbContext).Assembly.FullName);
                }));

        services.AddScoped<IPricingRecordRepository, PricingRecordRepository>();
        services.AddScoped<IUploadBatchRepository, UploadBatchRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ICsvParserService, CsvParserService>();

        return services;
    }
}

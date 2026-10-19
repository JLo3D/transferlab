using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TransferLab.Application.Abstractions.Persistence;
using TransferLab.Infrastructure.Persistence;
using TransferLab.Infrastructure.Persistence.Repositories;

namespace TransferLab.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration
                                   .GetConnectionString("TransferLab")
                               ?? throw new InvalidOperationException(
                                   "Connection string 'TransferLab' was not found.");

        services.AddDbContext<TransferLabDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<ITransferRepository, TransferRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
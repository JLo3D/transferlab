using Microsoft.Extensions.DependencyInjection;
using TransferLab.Application.Accounts.CreateAccount;
using TransferLab.Application.Transfers.CreateTransfer;
using TransferLab.Application.Transfers.ProcessTransfer;
using TransferLab.Application.Accounts.GetAccount;

namespace TransferLab.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddScoped<CreateAccountHandler>();
        services.AddScoped<CreateTransferHandler>();
        services.AddScoped<ProcessTransferHandler>();
        services.AddScoped<GetAccountHandler>();

        return services;
    }
}
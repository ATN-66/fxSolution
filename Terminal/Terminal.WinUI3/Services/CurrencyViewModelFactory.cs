using Microsoft.Extensions.DependencyInjection;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.ViewModels;

namespace Terminal.WinUI3.Services;

public class CurrencyViewModelFactory : ICurrencyViewModelFactory
{
    private readonly IServiceProvider _serviceProvider;

    public CurrencyViewModelFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public CurrencyViewModel Create()
    {
        return _serviceProvider.GetRequiredService<CurrencyViewModel>();
    }
}

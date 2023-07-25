using Microsoft.Extensions.DependencyInjection;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.ViewModels;

namespace Terminal.WinUI3.Services;

public class SymbolOfCurrencyViewModelFactory : ISymbolOfCurrencyViewModelFactory
{
    private readonly IServiceProvider _serviceProvider;

    public SymbolOfCurrencyViewModelFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public SymbolOfCurrencyViewModel Create()
    {
        return _serviceProvider.GetRequiredService<SymbolOfCurrencyViewModel>();
    }
}
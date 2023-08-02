using Microsoft.Extensions.DependencyInjection;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.ViewModels;

namespace Terminal.WinUI3.Services;

public class SymbolViewModelFactory : ISymbolViewModelFactory
{
    private readonly IServiceProvider _serviceProvider;

    public SymbolViewModelFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public SymbolViewModel Create()
    {
        return _serviceProvider.GetRequiredService<SymbolViewModel>();
    }
}
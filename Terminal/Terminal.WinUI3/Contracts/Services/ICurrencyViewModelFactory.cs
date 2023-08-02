using Terminal.WinUI3.ViewModels;

namespace Terminal.WinUI3.Contracts.Services;

public interface ISymbolViewModelFactory
{
    SymbolViewModel Create();
}
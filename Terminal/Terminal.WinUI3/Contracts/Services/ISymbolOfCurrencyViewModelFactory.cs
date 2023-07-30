using Terminal.WinUI3.ViewModels;

namespace Terminal.WinUI3.Contracts.Services;

public interface ICurrencyViewModelFactory
{
    CurrencyViewModel Create();
}
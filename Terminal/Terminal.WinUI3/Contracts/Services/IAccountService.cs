using Fx.Grpc;
using Terminal.WinUI3.Models.Account;
using Symbol = Common.Entities.Symbol;

namespace Terminal.WinUI3.Contracts.Services;

public interface IAccountService
{
    void ProcessProperties(string details);
    void ProcessMaxVolumes(string maxVolumes);
    void ProcessTickValues(string tickValues);
    GeneralRequest GetOpenPositionRequest(Symbol symbol, bool isReversed);
    AccountInfo GetAccountInfo();
}
using System.ComponentModel;
using Fx.Grpc;
using Terminal.WinUI3.Models.Account;
using Terminal.WinUI3.Models.Account.Enums;
using Terminal.WinUI3.Models.Trade;
using Symbol = Common.Entities.Symbol;

namespace Terminal.WinUI3.Contracts.Services;

public interface IAccountService : INotifyPropertyChanged
{
    ServiceState ServiceState { get; }
    AccountInfo GetAccountInfo();
    void ProcessProperties(string details);
    GeneralRequest GetOpenPositionRequest(Symbol symbol, bool isReversed);
    GeneralRequest GetClosePositionRequest(Symbol symbol, bool isReversed);
    void OpenPosition(int ticket, ResultCode code, string details);
    void ClosePosition(int ticket, ResultCode code, string details); 
    void OpenTransaction(int ticket, ResultCode code, string details);
    void CloseTransaction(int ticket, ResultCode code, string details);
    void UpdatePosition(int ticket, ResultCode code, string details); 
    IEnumerable<HistoryPosition> ProcessPositionsHistory(string details);
}
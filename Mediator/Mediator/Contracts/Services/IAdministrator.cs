///*+------------------------------------------------------------------+
//  |                                       Mediator.Contracts.Services|
//  |                                                IAdministrator.cs |
//  +------------------------------------------------------------------+*/

//using System.ComponentModel;
//using Common.Entities;

//namespace Mediator.Contracts.Services;

//public interface IAdministrator : INotifyPropertyChanged
//{ 
//    Task OnIndicatorConnectedAsync(Symbol symbol, Workplace workplace);
//    Task OnIndicatorDisconnectedAsync(DeInitReason reason);
//    bool IndicatorsConnected { get; }
//    bool IsIndicatorConnected(int index);
//    Workplace Workplace { get; }
////}
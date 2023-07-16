using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Fx.Grpc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Helpers;
using Terminal.WinUI3.Models.Account;
using Terminal.WinUI3.Models.Account.Enums;
using Terminal.WinUI3.Models.Trade;
using Terminal.WinUI3.Models.Trade.Enums;
using Enum = System.Enum;
using Symbol = Common.Entities.Symbol;
using TradeRequest = Fx.Grpc.TradeRequest;

namespace Terminal.WinUI3.Services;

internal sealed class AccountService : IAccountService
{
    private readonly ILogger<IAccountService> _logger;
    private readonly AccountInfo _accountInfo = new();
    private readonly List<Position> _positions = new();
    private readonly StringBuilder _detailsBuilder = new();
    private ServiceState _serviceState = ServiceState.Off;
    private readonly string _mT5DateTimeFormat;

    public AccountService(IConfiguration configuration, ILogger<IAccountService> logger)
    {
        _logger = logger;
        _accountInfo.FreeMarginPercentToUse = double.Parse(configuration.GetValue<string>("FreeMarginPercentToUse")!);
        _accountInfo.FreeMarginPercentToRisk = double.Parse(configuration.GetValue<string>("FreeMarginPercentToRisk")!);
        _accountInfo.Deviation = ulong.Parse(configuration.GetValue<string>("Deviation")!);

        _mT5DateTimeFormat = configuration.GetValue<string>($"{nameof(_mT5DateTimeFormat)}")!;
    }

    public ServiceState ServiceState
    {
        get => _serviceState;
        private set
        {
            if (value == ServiceState)
            {
                return;
            }

            _serviceState = value;
            OnPropertyChanged();
        }
    }
    
    public AccountInfo GetAccountInfo()
    {
        return _accountInfo;
    }
    public void ProcessProperties(string details)
    {
        var properties = details.Split(',');
        foreach (var property in properties)
        {
            var keyAndValue = property.Split(':');
            if (keyAndValue.Length != 2)
            {
                throw new ArgumentException("keyAndValue.Length != 2");
            }
            var propertyName = keyAndValue[0].Trim();
            var propertyValue = keyAndValue[1].Trim();
            var propertyInfo = _accountInfo.GetType().GetProperty(propertyName);
            if (propertyInfo == null)
            {
                throw new ArgumentException("propertyInfo == null"); 
            }
            object convertedValue;
            if (propertyInfo.PropertyType == typeof(int))
            {
                convertedValue = int.Parse(propertyValue);
            }
            else if (propertyInfo.PropertyType == typeof(double))
            {
                convertedValue = double.Parse(propertyValue);
            }
            else if (propertyInfo.PropertyType == typeof(bool))
            {
                convertedValue = bool.Parse(propertyValue);
            }
            else if (propertyInfo.PropertyType == typeof(long))
            {
                convertedValue = long.Parse(propertyValue);
            }
            else if (propertyInfo.PropertyType.IsEnum)
            {
                convertedValue = Enum.Parse(propertyInfo.PropertyType, propertyValue);
            }
            else
            {
                convertedValue = propertyValue;
            }

            propertyInfo.SetValue(_accountInfo, convertedValue);
        }

        ServiceState = ServiceState.ReadyToOpen;
    }

    private void CreatePosition(Symbol symbol, TradeType tradeType)
    {
        if (_positions.Count == 0 || _positions[^1].PositionState == PositionState.Closed)
        {
            _positions.Add(new Position(symbol, tradeType, _accountInfo.Deviation, _accountInfo.FreeMarginPercentToUse, _accountInfo.FreeMarginPercentToRisk, 7077, "Order opened by EXECUTIVE EA"));
        }
        else 
        {
            throw new InvalidOperationException("_positions.Count != 0 || _positions[^1].PositionState != PositionState.Closed");
        }
    }
    public GeneralRequest GetOpenPositionRequest(Symbol symbol, bool isReversed)
    {
        CreatePosition(symbol, isReversed ? TradeType.Buy : TradeType.Sell);
        ServiceState = ServiceState.Busy;

        var request = new GeneralRequest
        {
            Type = MessageType.TradeCommand,
            TradeRequest = new TradeRequest { TradeCode = TradeCode.OpenPosition, Ticket = 666 }
        };

        _detailsBuilder.Clear();
        _detailsBuilder.Append($"{nameof(Symbol)}>").Append(_positions[^1].Symbol);
        _detailsBuilder.Append($";{nameof(TradeType)}>").Append(_positions[^1].OpeningOrder.TradeType);
        _detailsBuilder.Append(";Deviation>").Append(_positions[^1].OpeningOrder.Deviation);
        _detailsBuilder.Append(";FreeMarginPercentToUse>").Append(_positions[^1].OpeningOrder.FreeMarginPercentToUse);
        _detailsBuilder.Append(";FreeMarginPercentToRisk>").Append(_positions[^1].OpeningOrder.FreeMarginPercentToRisk);
        _detailsBuilder.Append(";MagicNumber>").Append(_positions[^1].MagicNumber);
        _detailsBuilder.Append(";Comment>").Append(_positions[^1].OpeningOrder.Comment);
        var details = _detailsBuilder.ToString();
        request.TradeRequest.Details = details;
        return request;
    }
    public GeneralRequest GetClosePositionRequest(Symbol symbol, bool isReversed)// todo: Symbol symbol, bool isReversed ???
    {
        if (_positions[^1].PositionState != PositionState.Opened)
        {
            throw new InvalidOperationException("PositionState != PositionState.Opened");
        }

        _positions[^1].PositionState = PositionState.ToBeClosed;
        _positions[^1].ClosingOrder = new ClosingOrder(_positions[^1].Ticket, _accountInfo.Deviation);
        ServiceState = ServiceState.Busy;

        var request = new GeneralRequest
        {
            Type = MessageType.TradeCommand,
            TradeRequest = new TradeRequest { TradeCode = TradeCode.ClosePosition, Ticket = 666 }
        };

        _detailsBuilder.Clear();
        _detailsBuilder.Append("ticket>").Append(_positions[^1].ClosingOrder.TicketToClose);
        _detailsBuilder.Append(";deviation>").Append(_positions[^1].ClosingOrder.Deviation);
        var details = _detailsBuilder.ToString();
        request.TradeRequest.Details = details; 
        return request;
    }
    public void OpenPosition(int ticket, ResultCode code, string details)
    {
        if (code == ResultCode.Success)
        {
            _positions[^1].PositionState = PositionState.Opened;
            ServiceState = ServiceState.ReadyToClose;
        }
        else
        {
            throw new NotImplementedException();
        }
    }
    public void ClosePosition(int ticket, ResultCode code, string details)
    {
        if (code == ResultCode.Success)
        {
            _positions[^1].PositionState = PositionState.Closed;
            ServiceState = ServiceState.ReadyToOpen;
        }
        else
        {
            throw new NotImplementedException();
        }
    }
    public void OpenTransaction(int ticket, ResultCode code, string details)
    {
        if (code == ResultCode.Success)
        {
            UpdateOpen(details);
        }
        else
        {
            throw new NotImplementedException();
        }
    }
    public void CloseTransaction(int ticket, ResultCode code, string details)
    {
        if (code == ResultCode.Success)
        {
            UpdateClose(details);
        }
        else
        {
            throw new NotImplementedException();
        }
    }
    private void UpdateOpen(string details)
    {
        var detailsDictionary = details.Split(',').Select(part => part.Split('>')).ToDictionary(split => split[0].Trim(), split => split[1].Trim());
        switch (_positions[^1].PositionState)
        {
            case PositionState.Opened:
                _positions[^1].OpeningDeal = new Deal();
                if (Enum.TryParse<Symbol>(detailsDictionary["PositionSymbol"], out var symbol))
                {
                    if (symbol != _positions[^1].Symbol)
                    {
                        throw new ArgumentException("symbol != Symbol");
                    }
                }
                else
                {
                    throw new Exception("Enum.TryParse<Symbol>");
                }

                var positionTradeType = detailsDictionary["PositionTradeType"].GetEnumValueFromDescription<TradeType>();
                if (_positions[^1].TradeType != positionTradeType)
                {
                    throw new ArgumentException("TradeType != tradeType");
                }

                _positions[^1].Ticket = ulong.Parse(detailsDictionary["OpeningOrderTicket"]); // OpeningOrderTicket>19926499
                _positions[^1].OpeningOrder.Ticket = ulong.Parse(detailsDictionary["OpeningOrderTicket"]); // OpeningOrderTicket>19926499
                _positions[^1].OpeningDeal.Ticket = ulong.Parse(detailsDictionary["OpeningDealTicket"]); // OpeningDealTicket>17797372
                _positions[^1].OpeningDeal.OrderAction = detailsDictionary["OpeningDealOrderAction"].GetEnumValueFromDescription<OrderAction>(); // DealOrderAction>TRADE_ACTION_DEAL
                _positions[^1].OpeningDeal.OrderFilling = detailsDictionary["OpeningDealFilling"].GetEnumValueFromDescription<OrderFilling>(); // OpeningDealFilling>ORDER_FILLING_FOK
                _positions[^1].OpeningDeal.DealType = detailsDictionary["DealType"].GetEnumValueFromDescription<DealType>(); // DealType>DEAL_TYPE_SELL
                _positions[^1].OpeningDeal.TimeType = detailsDictionary["OpeningDealTimeType"].GetEnumValueFromDescription<TimeType>(); // OpeningDealTimeType>ORDER_TIME_GTC

                if (int.TryParse(detailsDictionary["OpeningDealRetcode"], out var retcode) && Enum.IsDefined(typeof(TradeServerReturnCode), retcode))
                {
                    _positions[^1].OpeningDeal.TradeServerReturnCode = (TradeServerReturnCode)retcode;
                }
                else
                {
                    throw new Exception("Enum.TryParse<TradeServerReturnCode>");
                }

                _positions[^1].OpeningDeal.Price = double.Parse(detailsDictionary["OpeningDealPrice"]); // OpeningDealPrice>1.12004
                _positions[^1].OpeningDeal.StopLoss = double.Parse(detailsDictionary["OpeningDealStopLoss"]); // OpeningDealStopLoss>1.12255
                _positions[^1].OpeningDeal.TakeProfit = double.Parse(detailsDictionary["OpeningDealTakeProfit"]); // OpeningDealTakeProfit>1.11753
                _positions[^1].OpeningDeal.Ask = double.Parse(detailsDictionary["OpeningDealAsk"]); // OpeningDealAsk>1.1201
                _positions[^1].OpeningDeal.Bid = double.Parse(detailsDictionary["OpeningDealBid"]); // OpeningDealBid>1.12004
                _positions[^1].OpeningDeal.Volume = double.Parse(detailsDictionary["OpeningDealVolume"]); // OpeningDealVolume>1.01
                _positions[^1].OpeningDeal.Time = DateTime.ParseExact(detailsDictionary["OpeningDealTime"], _mT5DateTimeFormat, CultureInfo.InvariantCulture); // OpeningDealTime>2023.07.19 22:02:29
                break;
           
            case PositionState.ToBeOpened:
            case PositionState.ToBeClosed:
            case PositionState.RejectedToBeOpened:
            case PositionState.NaN:
            case PositionState.Closed:
            default: throw new ArgumentOutOfRangeException($"{_positions[^1].PositionState}");
        }
    }
    private void UpdateClose(string details)
    {
        if (_positions[^1].PositionState == PositionState.Opened)
        {
            _positions[^1].PositionState = PositionState.Closed;
            ServiceState = ServiceState.ReadyToOpen;
            _positions[^1].ClosingOrder = new ClosingOrder(_positions[^1].Ticket, _accountInfo.Deviation);
        }

        var detailsDictionary = details.Split(',').Select(part => part.Split('>')).ToDictionary(split => split[0].Trim(), split => split[1].Trim());
        switch (_positions[^1].PositionState)
        {
            case PositionState.Closed:
                _positions[^1].ClosingDeal = new Deal
                {
                    DealType = detailsDictionary["DealType"].GetEnumValueFromDescription<DealType>() // DealType > DEAL_TYPE_BUY
                };
                if (_positions[^1].OpeningDeal.DealType == _positions[^1].ClosingDeal.DealType)
                {
                    throw new InvalidOperationException("_positions[^1].OpeningDeal.DealType == _positions[^1].ClosingDeal.DealType");
                }
                if (int.TryParse(detailsDictionary["ClosingDealRetcode"], out var retcode) && Enum.IsDefined(typeof(TradeServerReturnCode), retcode)) // ClosingDealRetcode > 10009
                {
                    _positions[^1].ClosingDeal.TradeServerReturnCode = (TradeServerReturnCode)retcode;
                }
                else
                {
                    throw new Exception("Enum.TryParse<TradeServerReturnCode>");
                }

                _positions[^1].ClosingOrder.Ticket = ulong.Parse(detailsDictionary["ClosingOrderTicket"]); // ClosingDealTicket > 17798280
                _positions[^1].ClosingDeal.Ticket = ulong.Parse(detailsDictionary["ClosingDealTicket"]); // ClosingDealTicket>17798131
                _positions[^1].ClosingDeal.Price = double.Parse(detailsDictionary["ClosingDealPrice"]); // ClosingDealPrice>1.12044
                _positions[^1].ClosingDeal.Ask = double.Parse(detailsDictionary["ClosingDealAsk"]); // ClosingDealAsk>1.12044
                _positions[^1].ClosingDeal.Bid = double.Parse(detailsDictionary["ClosingDealBid"]); // ClosingDealBid>1.12038
                _positions[^1].ClosingDeal.Time = DateTime.ParseExact(detailsDictionary["ClosingDealTime"], _mT5DateTimeFormat, CultureInfo.InvariantCulture); // ClosingDealTime>2023.07.20 00:04:04
                var magicNumber = ulong.Parse(detailsDictionary["PositionMagic"]); // PositionMagic > 7077
                if (magicNumber != _positions[^1].MagicNumber)
                {
                    //position closed manually in MT5 Terminal
                    //throw new InvalidOperationException("magicNumber != _positions[^1].MagicNumber");
                }
                break;
            case PositionState.Opened:
            case PositionState.ToBeOpened:
            case PositionState.ToBeClosed:
            case PositionState.RejectedToBeOpened:
            case PositionState.NaN:
            default: throw new ArgumentOutOfRangeException();
        }
    }
    public void UpdatePosition(int ticket, ResultCode code, string details)
    {
        if (code == ResultCode.Success)
        {
            var detailsDictionary = details.Split(',').Select(part => part.Split('>')).ToDictionary(split => split[0].Trim(), split => split[1].Trim());
            if (_positions[^1].PositionState != PositionState.Opened)
            {
                throw new InvalidOperationException("_positions[^1].PositionState != PositionState.Opened");
            }

            var positionTicket = ulong.Parse(detailsDictionary["PositionTicket"]); // PositionTicket>19928000
            if (positionTicket != _positions[^1].Ticket)
            {
                throw new InvalidOperationException("positionTicket != _positions[^1].Ticket");
            }
            _positions[^1].OpeningDeal.StopLoss = double.Parse(detailsDictionary["OpeningDealStopLoss"]); // OpeningDealStopLoss>1.12255
            _positions[^1].OpeningDeal.TakeProfit = double.Parse(detailsDictionary["OpeningDealTakeProfit"]); // OpeningDealTakeProfit>1.11753
        }
        else
        {
            throw new NotImplementedException();
        }
    }
    public IEnumerable<HistoryPosition> ProcessPositionsHistory(string details)
    {
        var positions = new List<HistoryPosition>();
        var tradingHistory = Newtonsoft.Json.JsonConvert.DeserializeObject<TradingHistory>(details)!;
        var deals = tradingHistory.HistoryDeals;
        var orders = tradingHistory.HistoryOrders;

        var dealGroups = deals.GroupBy(d => d.Position).OrderBy(g => g.First().Time);
        var orderGroups = orders.GroupBy(o => o.Position).OrderBy(g => g.First().TimeSetup).ToDictionary(g => g.Key);

        foreach (var dealGroup in dealGroups)
        {
            var startDeal = dealGroup.First();
            if ((DealType)startDeal.Type is not (DealType.Buy or DealType.Sell))
            {
                continue;
            }

            var position = new HistoryPosition(dealGroup.Key);

            var startOrderGroup = orderGroups[dealGroup.Key];
            position.StartDeal = startDeal;
            position.StartOrder = startOrderGroup.First();
            Debug.Assert(position.StartDeal.Order == position.StartOrder.Ticket);

            var endDeal = dealGroup.Last();
            var endOrderGroup = orderGroups[dealGroup.Key];
            position.EndDeal = endDeal;
            position.EndOrder = endOrderGroup.Last();
            Debug.Assert(position.EndDeal.Order == position.EndOrder.Ticket);

            positions.Add(position);
        }

        positions = positions.OrderBy(p => p.StartDeal.Time).ToList();


        if (positions.Count * 2 != orders.Count)
        {
            throw new InvalidOperationException("positions.Count * 2 != orders.Count");
        }

        return positions;
    }


    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
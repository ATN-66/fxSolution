using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using CommunityToolkit.Mvvm.Messaging;
using Fx.Grpc;
using Microsoft.Extensions.Configuration;

using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Helpers;
using Terminal.WinUI3.Messenger.AccountService;
using Terminal.WinUI3.Messenger.Chart;
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
    private readonly AccountInfo _accountInfo = new();
    private Position? _position;
    private readonly StringBuilder _detailsBuilder = new();
    private ServiceState _serviceState = ServiceState.Off;
    private readonly string _mT5DateTimeFormat;

    public AccountService(IConfiguration configuration)
    {
        _accountInfo.FreeMarginPercentToUse = double.Parse(configuration.GetValue<string>("FreeMarginPercentToUse")!);
        _accountInfo.FreeMarginPercentToRisk = double.Parse(configuration.GetValue<string>("FreeMarginPercentToRisk")!);
        _accountInfo.Deviation = ulong.Parse(configuration.GetValue<string>("Deviation")!);
        _mT5DateTimeFormat = configuration.GetValue<string>($"{nameof(_mT5DateTimeFormat)}")!;

        WeakReferenceMessenger.Default.Register<OrderRequestMessage>(this, OnOrderRequestAsync);
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

    public Symbol Symbol => _position!.Symbol;
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
        _position = new Position(symbol, tradeType, _accountInfo.Deviation, 666, _accountInfo.FreeMarginPercentToUse, _accountInfo.FreeMarginPercentToRisk);
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
        _detailsBuilder.Append($"{nameof(Symbol).ToLower()}>").Append(_position!.Symbol);
        _detailsBuilder.Append($";{nameof(TradeType).ToLower()}>").Append(_position.StartOrder.TradeType);
        _detailsBuilder.Append(";deviation>").Append(_position.Deviation);
        _detailsBuilder.Append(";freeMarginPercentToUse>").Append(_position.FreeMarginPercentToUse);
        _detailsBuilder.Append(";freeMarginPercentToRisk>").Append(_position.FreeMarginPercentToRisk);
        _detailsBuilder.Append(";magicNumber>").Append(_position.MagicNumber);
        _detailsBuilder.Append(";comment>").Append(_position.StartOrder.Comment);
        var details = _detailsBuilder.ToString();
        request.TradeRequest.Details = details;
        return request;
    }
    public GeneralRequest GetClosePositionRequest(Symbol symbol, bool isReversed)// todo: Symbol symbol, bool isReversed // keep or remove???
    {
        if (_position!.PositionState != PositionState.Opened)
        {
            throw new InvalidOperationException("PositionState != PositionState.Opened");
        }

        _position.PositionState = PositionState.ToBeClosed;
        ServiceState = ServiceState.Busy;

        var request = new GeneralRequest
        {
            Type = MessageType.TradeCommand,
            TradeRequest = new TradeRequest { TradeCode = TradeCode.ClosePosition, Ticket = 666 }
        };

        _detailsBuilder.Clear();
        _detailsBuilder.Append("ticket>").Append(_position.Ticket);
        _detailsBuilder.Append(";deviation>").Append(_position.Deviation);
        var details = _detailsBuilder.ToString();
        request.TradeRequest.Details = details;
        return request;
    }
    public GeneralRequest GetModifyPositionRequest(Symbol symbol, double stopLoss, double takeProfit)
    {
        if (_position!.PositionState != PositionState.Opened)
        {
            throw new InvalidOperationException("PositionState != PositionState.Opened");
        }

        if (_position!.Symbol != symbol)
        {
            throw new InvalidOperationException("_position!.Symbol != symbol");
        }

        ServiceState = ServiceState.Busy;

        var request = new GeneralRequest
        {
            Type = MessageType.TradeCommand,
            TradeRequest = new TradeRequest { TradeCode = TradeCode.ModifyPosition, Ticket = 666 }
        };

        _detailsBuilder.Clear();
        _detailsBuilder.Append("ticket>").Append(_position.Ticket);
        _detailsBuilder.Append(";symbol>").Append(symbol);
        _detailsBuilder.Append(";stopLoss>").Append(stopLoss);
        _detailsBuilder.Append(";takeProfit>").Append(takeProfit);
        var details = _detailsBuilder.ToString();
        request.TradeRequest.Details = details;
        return request;
    }
    public void OpenPosition(int ticket, ResultCode code, string details)
    {
        if (code == ResultCode.Success)
        {
            _position!.PositionState = PositionState.Opened;
            ServiceState = ServiceState.ReadyToClose;
        }
        else
        {
            TurnOff(details);
        }
    }
    public void ClosePosition(int ticket, ResultCode code, string details)
    {
        if (code == ResultCode.Success)
        {
            _position!.PositionState = PositionState.Closed;
            ServiceState = ServiceState.ReadyToOpen;
        }
        else
        {
            TurnOff(details);
        }
    }
    public void ModifyPosition(int ticket, ResultCode code, string details)
    {
        if (code == ResultCode.Success)
        {
            ServiceState = ServiceState.ReadyToClose;
        }
        else
        {
            TurnOff(details);
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
            TurnOff(details);
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
            TurnOff(details);
        }
    }
    private void UpdateOpen(string details)
    {
        if (_position!.PositionState != PositionState.Opened)
        {
            throw new InvalidOperationException("PositionState != PositionState.Opened");
        }
        var detailsDictionary = details.Split(',').Select(part => part.Split('>')).ToDictionary(split => split[0].Trim(), split => split[1].Trim());

        if (Enum.TryParse<Symbol>(detailsDictionary["PositionSymbol"], out var symbol))
        {
            if (symbol != _position.Symbol)
            {
                throw new ArgumentException("symbol != Symbol");
            }
        }
        else
        {
            throw new Exception("Enum.TryParse<Symbol>");
        }

        var orderTradeType = detailsDictionary["StartOrderTradeType"].GetEnumValueFromDescription<TradeType>();
        if (_position.StartOrder.TradeType != orderTradeType)
        {
            throw new ArgumentException("_position.StartOrder.TradeType != orderTradeType");
        }

        _position.Ticket = ulong.Parse(detailsDictionary["StartOrderTicket"]);
        _position.StartOrder.Ticket = ulong.Parse(detailsDictionary["StartOrderTicket"]);
        _position.StartOrder.Price = double.Parse(detailsDictionary["StartOrderPrice"]);
        _position.StartOrder.StopLoss = double.Parse(detailsDictionary["StartOrderStopLoss"]);
        _position.StartOrder.TakeProfit = double.Parse(detailsDictionary["StartOrderTakeProfit"]);
        _position.StartOrder.Ask = double.Parse(detailsDictionary["StartOrderAsk"]);
        _position.StartOrder.Bid = double.Parse(detailsDictionary["StartOrderBid"]);
        _position.StartOrder.Volume = double.Parse(detailsDictionary["StartOrderVolume"]);
        _position.StartOrder.Time = DateTime.ParseExact(detailsDictionary["StartOrderTime"], _mT5DateTimeFormat, CultureInfo.InvariantCulture);

        StrongReferenceMessenger.Default.Send(new OrderAcceptMessage(_position.Symbol, AcceptType.Open, _position.StartOrder), new SymbolToken(_position.Symbol) as CommunicationToken);
    }
    private void UpdateClose(string details)
    {
        if (_position!.PositionState == PositionState.Opened)
        {
            _position.PositionState = PositionState.Closed;
            ServiceState = ServiceState.ReadyToOpen;
        }

        var detailsDictionary = details.Split(',').Select(part => part.Split('>')).ToDictionary(split => split[0].Trim(), split => split[1].Trim());
        if (Enum.TryParse<Symbol>(detailsDictionary["PositionSymbol"], out var symbol))
        {
            if (symbol != _position.Symbol)
            {
                throw new ArgumentException("symbol != Symbol");
            }
        }
        else
        {
            throw new Exception("Enum.TryParse<Symbol>");
        }

        var orderTradeType = detailsDictionary["EndOrderTradeType"].GetEnumValueFromDescription<TradeType>();
        if (_position.EndOrder.TradeType != orderTradeType)
        {
            throw new ArgumentException("_position.EndOrder.TradeType != orderTradeType");
        }

        _position.EndOrder.Ticket = ulong.Parse(detailsDictionary["EndOrderTicket"]);
        _position.EndOrder.Price = double.Parse(detailsDictionary["EndOrderPrice"]);
        _position.EndOrder.Ask = double.Parse(detailsDictionary["EndOrderAsk"]);
        _position.EndOrder.Bid = double.Parse(detailsDictionary["EndOrderBid"]);
        _position.EndOrder.Time = DateTime.ParseExact(detailsDictionary["EndOrderTime"], _mT5DateTimeFormat, CultureInfo.InvariantCulture);

        StrongReferenceMessenger.Default.Send(new OrderAcceptMessage(_position.Symbol, AcceptType.Close, _position.EndOrder), new SymbolToken(_position.Symbol) as CommunicationToken);
        _position = null;
    }
    public void UpdateTransaction(int ticket, ResultCode code, string details)
    {
        if (code == ResultCode.Success)
        {
            var detailsDictionary = details.Split(',').Select(part => part.Split('>')).ToDictionary(split => split[0].Trim(), split => split[1].Trim());
            if (_position!.PositionState != PositionState.Opened)
            {
                throw new InvalidOperationException("_position.PositionState != PositionState.Opened");
            }

            var positionTicket = ulong.Parse(detailsDictionary["PositionTicket"]); 
            if (positionTicket != _position.Ticket)
            {
                throw new InvalidOperationException("positionTicket != _position.Ticket");
            }
            _position.StartOrder.StopLoss = double.Parse(detailsDictionary["StartOrderStopLoss"]);
            _position.StartOrder.TakeProfit = double.Parse(detailsDictionary["StartOrderTakeProfit"]);

            StrongReferenceMessenger.Default.Send(new OrderAcceptMessage(_position.Symbol, AcceptType.Modify, _position.StartOrder), new SymbolToken(_position.Symbol) as CommunicationToken);
        }
        else
        {
            throw new NotImplementedException("AccountService.UpdateTransaction:code != ResultCode.Success");
        }
    }
    private void OnOrderRequestAsync(object recipient, OrderRequestMessage message)
    {
        if (_position == null)
        {
            message.Reply(Order.Null);

        }
        else if (_position.Symbol != message.Symbol)
        {
            message.Reply(Order.Null);
        }
        else
        {
            message.Reply(_position.StartOrder);
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
            throw new InvalidOperationException("positions.TicksCount * 2 != orders.TicksCount");
        }

        return positions;
    }
    private void TurnOff(string details)
    {
        Debug.WriteLine($"details: {details}");
        ServiceState = ServiceState.Off;
        _position = null;
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
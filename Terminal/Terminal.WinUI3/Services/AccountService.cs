using System.Text;
using Fx.Grpc;
using Microsoft.Extensions.Configuration;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Models.Account;
using Terminal.WinUI3.Models.Trade;
using Enum = System.Enum;
using Symbol = Common.Entities.Symbol;
using TradeRequest = Fx.Grpc.TradeRequest;

namespace Terminal.WinUI3.Services;

internal class AccountService : IAccountService
{
    private readonly AccountInfo _accountInfo = new();
    private readonly Dictionary<Symbol, double> _maxVolumes = new();
    private readonly Dictionary<Symbol, double> _tickValues = new();
    private readonly Dictionary<Symbol, int> _stopLossInPips = new();
    private readonly StringBuilder _detailsBuilder = new();
    private readonly double _riskPercent;
    private double RiskAmount => _riskPercent / 100 * _accountInfo.Balance;

    public AccountService(IConfiguration configuration)
    {
        _riskPercent = double.Parse(configuration.GetValue<string>($"{nameof(_riskPercent)}")!);
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
    }
    public void ProcessMaxVolumes(string maxVolumes)
    {
        // EURUSD: 1.50, GBPUSD: 1.28, USDJPY: 1.68, EURGBP: 1.12, EURJPY: 1.12, GBPJPY: 0.96
        var entries = maxVolumes.Split(',');
        foreach (var entry in entries)
        {
            var parts = entry.Split(':');
            var symbolStr = parts[0].Trim();
            var volume = double.Parse(parts[1].Trim());
            if (!Enum.TryParse(symbolStr, out Symbol symbol))
            {
                throw new Exception($"{symbolStr}: !Enum.TryParse(symbolStr, out Symbol symbol)");
            }

            _maxVolumes[symbol] = volume;
        }
    }
    public void ProcessTickValues(string tickValues)
    {
        // EURUSD:1.32, GBPUSD:1.32, USDJPY:0.95, EURGBP:1.73, EURJPY:0.95, GBPJPY:0.95
        var entries = tickValues.Split(',');
        foreach (var entry in entries)
        {
            var parts = entry.Split(':');
            var symbolStr = parts[0].Trim();
            var value = double.Parse(parts[1].Trim());
            if (!Enum.TryParse(symbolStr, out Symbol symbol))
            {
                throw new Exception($"{symbolStr}: !Enum.TryParse(symbolStr, out Symbol symbol)");
            }

            _tickValues[symbol] = value;
            _stopLossInPips[symbol] = (int)(RiskAmount / (_maxVolumes[symbol] * _tickValues[symbol] * 10));
        }
    }
    public GeneralRequest GetOpenPositionRequest(Symbol symbol, bool isReversed)
    {
        var request = new GeneralRequest
        {
            Type = MessageType.TradeCommand,
            TradeRequest = new TradeRequest { Code = TradeCode.OpenPosition, Ticket = 666 }
        };

        _detailsBuilder.Clear();
        _detailsBuilder.Append($"{nameof(symbol)}>").Append(symbol);

        var orderType = isReversed ? OrderType.OrderTypeBuy : OrderType.OrderTypeSell;
        _detailsBuilder.Append($";{nameof(orderType).ToLower()}>").Append(orderType);

        var volume = _maxVolumes[symbol];
        _detailsBuilder.Append($";{nameof(volume)}>").Append(volume.ToString("###.00"));
        
        _detailsBuilder.Append($";stoplossinpips>").Append(_stopLossInPips[symbol]);

        var details = _detailsBuilder.ToString(); // "symbol>EURUSD;ordertype>OrderTypeSell;volume>1.50;stoplossinpips>25"

        request.TradeRequest.Details = details;
        return request;
    }
}
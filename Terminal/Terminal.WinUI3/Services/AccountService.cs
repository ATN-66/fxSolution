using Common.Entities;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Models.Account;

namespace Terminal.WinUI3.Services;

internal class AccountService : IAccountService
{
    private readonly AccountInfo _accountInfo = new();
    private readonly Dictionary<Symbol, double> _maxVolumes = new();

    public void SetUpAccountInfo(int ticket, string details)
    {
        switch (ticket)
        {
            case 1 or 2 or 3:
                ProcessProperties(details);
                break;
            case 4:
                ProcessMaxVolumes(details);
                break;
        }
    }

    private void ProcessProperties(string details)
    {
        var properties = details.Split(',');
        foreach (var property in properties)
        {
            var keyAndValue = property.Split(':');
            if (keyAndValue.Length != 2)
            {
                continue;
            }

            var propertyName = keyAndValue[0].Trim();
            var propertyValue = keyAndValue[1].Trim();
            var propertyInfo = _accountInfo.GetType().GetProperty(propertyName);
            if (propertyInfo == null)
            {
                continue;
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
    private void ProcessMaxVolumes(string maxVolumes)
    {
        var entries = maxVolumes.Split(',');
        foreach (var entry in entries)
        {
            var parts = entry.Split(':');
            var symbolStr = parts[0].Trim();
            var volume = double.Parse(parts[1].Trim());
            if (Enum.TryParse(symbolStr, out Symbol symbol))
            {
                _maxVolumes[symbol] = volume;
            }
        }
    }
}
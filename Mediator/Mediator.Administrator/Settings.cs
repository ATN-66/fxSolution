/*+------------------------------------------------------------------+
  |                                              Mediator.Processors |
  |                                                 Settings.cs |
  +------------------------------------------------------------------+*/

using System.Dynamic;
using Common.Entities;

namespace Mediator.Administrator;

public class Settings
{
    public string ClientMediatorToTerminalHost
    {
        get
        {
            return "localhost";
        }
    }

    public int ClientMediatorToTerminalPort
    {
        get
        {
            return 8080;
        }
    } 
    
    public const string MultipleConnections = "Indicator cannot be connected more that one time.";
    public readonly int TotalIndicators = Enum.GetValues(typeof(Symbol)).Length;
    public readonly bool[] ConnectedIndicators;
    public readonly Space?[] Environments;
    public readonly DeInitReason?[] DeInitReasons;
    
    public Settings()
    {
        ConnectedIndicators = new bool[TotalIndicators];
        Environments = new Space?[TotalIndicators];
        DeInitReasons = new DeInitReason?[TotalIndicators];
    }

    public bool ExpertAdvisorConnected => true; // TODO: Expert Advisor

    public bool IndicatorsConnected
    {
        get
        {
            for (var i = 1; i <= TotalIndicators; i++)
            {
                if (ConnectedIndicators[i - 1]) continue;
                return false;
            }

            return true;
        }
    }

    public Space? Environment
    {
        get
        {
            var result = Environments[0];

            if (result == null)
                return null;

            for (var i = 1; i < TotalIndicators; i++)
            {
                if (Environments[i] == null || Environments[i]!.Value != result.Value)
                    return null;
            }

            return result;
        }
    }




    public event EventHandler? TerminalConnectedChanged;
    
    private bool _terminalConnected;
    public bool TerminalConnected
    {
        get => _terminalConnected;
        set
        {
            if (_terminalConnected == value) return;
            var wasOff = !_terminalConnected;
            _terminalConnected = value;
            if (wasOff && _terminalConnected)
            {
                TerminalConnectedChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
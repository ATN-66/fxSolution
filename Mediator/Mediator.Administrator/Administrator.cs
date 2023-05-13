/*+------------------------------------------------------------------+
  |                                              Mediator.Processors |
  |                                                 Administrator.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;
using Environment = Common.Entities.Environment;

namespace Mediator.Administrator;

public class Administrator
{
    public const string MultipleConnections = "Indicator cannot be connected more that one time.";
    public readonly int TotalIndicators = Enum.GetValues(typeof(Symbol)).Length;
    public readonly bool[] ConnectedIndicators;
    public readonly Environment?[] Environments;
    public readonly DeInitReason?[] DeInitReasons;
    
    public Administrator()
    {
        ConnectedIndicators = new bool[TotalIndicators];
        Environments = new Environment?[TotalIndicators];
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

    public Environment Environment
    {
        get
        {
            var result = Environments[0]!.Value;
            for (var i = 2; i <= TotalIndicators; i++)
            {
                if (Environments[i - 1]!.Value == result) continue;
                throw new Exception(nameof(Environment));
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
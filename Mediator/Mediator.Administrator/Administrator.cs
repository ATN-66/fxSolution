/*+------------------------------------------------------------------+
  |                                              Mediator.Processors |
  |                                                 Administrator.cs |
  +------------------------------------------------------------------+*/

using Environment = Common.Entities.Environment;

namespace Mediator.Administrator;

public class Administrator
{
    private Environment _environment;
    public Environment Environment
    {
        get => _environment;
        set
        {
            _environment = value;
            Console.WriteLine($"Environment: {_environment}");
        }
    }

    public event EventHandler? TerminalIsONChanged;
    private bool _terminalIsON;
    public bool IndicatorsIsON;
    public bool ExpertAdvisorIsON;

    public bool TerminalIsON
    {
        get => _terminalIsON;
        set
        {
            if (_terminalIsON == value) return;
            var wasOff = !_terminalIsON;
            _terminalIsON = value;
            if (wasOff && _terminalIsON)
            {
                TerminalIsONChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
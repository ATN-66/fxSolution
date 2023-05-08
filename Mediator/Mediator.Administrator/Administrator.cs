/*+------------------------------------------------------------------+
  |                                              Mediator.Processors |
  |                                                 Administrator.cs |
  +------------------------------------------------------------------+*/

namespace Mediator.Administrator;

public class Administrator
{
    public event EventHandler? TerminalIsONChanged;
    private bool _terminalIsON;

    public Common.Entities.Environment? Environment;
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
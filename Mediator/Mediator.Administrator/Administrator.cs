/*+------------------------------------------------------------------+
  |                                              Mediator.Processors |
  |                                                 Administrator.cs |
  +------------------------------------------------------------------+*/

using System.Configuration;
using System.Reflection;
using Environment = Common.Entities.Environment;

namespace Mediator.Administrator;

public class Administrator
{
    public Environment Environment;

    public event EventHandler? TerminalIsONChanged;
    private bool _terminalIsON;
    public bool IndicatorsIsON;
    public bool ExpertAdvisorIsON;

    public Administrator()
    {
        Environment = (Environment)Enum.Parse(typeof(Environment), ConfigurationManager.OpenExeConfiguration(Assembly.GetExecutingAssembly().Location).AppSettings.Settings[nameof(Environment)].Value);
        Console.WriteLine($"Environment: {Environment}.");
    }

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
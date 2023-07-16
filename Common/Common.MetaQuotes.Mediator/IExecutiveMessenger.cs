/*+------------------------------------------------------------------+
  |                                       Common.MetaQuotes.Mediator |
  |                                           IExecutiveMessenger.cs |
  +------------------------------------------------------------------+*/

using System.Threading.Tasks;

namespace Common.MetaQuotes.Mediator
{
    public interface IExecutiveMessenger
    {
        void DeInit(string dateTime);
        Task<string> InitAsync(string datetime);
        string Pulse(string dateTime, string type, string code, string ticket, string result, string details);
    }
}
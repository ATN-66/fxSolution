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
        Task<string> PulseAsync(string dateTime, string type, string code, string ticket, string details);
    }
}
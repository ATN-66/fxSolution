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
        string Pulse(string dateTime, int type, int code, string message);
    }
}
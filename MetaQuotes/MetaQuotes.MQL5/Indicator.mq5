//+------------------------------------------------------------------+
//|                                                    Indicator.mq4 |
//|       Copyright © 2023, Andrew Nikulin (andrew.nikulin@live.com) |
//+------------------------------------------------------------------+
string indicatorShortName = "Indicator";
#property copyright "Copyright © 2023, Andrew Nikulin (andrew.nikulin@live.com)"
#property version   "999.666"
#property indicator_chart_window

#import "MetaQuotes.Prototype.Indicator.PipeMethodCalls.dll"

int environment = 1; //Development = 1, Production = 4
int symbol;
int k;
string result;
bool connected = false;

int OnInit()
{
         if (Symbol() == "EURUSD") { symbol = 1; k = 100000; }
	else if (Symbol() == "GBPUSD") { symbol = 2; k = 100000; }
	else if (Symbol() == "USDJPY") { symbol = 3; k = 1000; }
	else if (Symbol() == "EURGBP") { symbol = 4; k = 100000; }
	else if (Symbol() == "EURJPY") { symbol = 5; k = 1000; }
	else if (Symbol() == "GBPJPY") { symbol = 6; k = 1000; }
	else { Print("This chart has a wrong Symbol."); PlaySound("disconnect.wav"); return INIT_FAILED; }
	
   result = IndicatorToMediatorService::Init(symbol, environment);
   if(result == "ok")
   {
      connected = true;
   }
   else
   {
      Print(result); PlaySound("disconnect.wav"); return INIT_SUCCEEDED;//if INIT_FAILED MT5 kicks off the indicator
   }
   Print(Symbol() + ":" + result);
   return(INIT_SUCCEEDED);
}

void OnTick()
{
   if(!connected) return;
   MqlTick last_tick;
   if(SymbolInfoTick(Symbol(), last_tick))
   {
      result = IndicatorToMediatorService::Tick(symbol, TimeToString(last_tick.time, TIME_DATE | TIME_SECONDS), last_tick.ask * k, last_tick.bid * k);
      if (result != "ok")
      {
         connected = false;
         Print(result); PlaySound("disconnect.wav");
      }
   }
   else Print("SymbolInfoTick() failed, error = ", GetLastError());
}

void OnDeinit(const int reason)
{
   if(!connected) return;
   IndicatorToMediatorService::DeInit(symbol, reason);
}
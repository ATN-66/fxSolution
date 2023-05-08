//+------------------------------------------------------------------+
//|                                                    Indicator.mq5 |
//|       Copyright © 2023, Andrew Nikulin (andrew.nikulin@live.com) |
//+------------------------------------------------------------------+
string indicatorShortName = "Indicator";
#property copyright "Copyright © 2023, Andrew Nikulin (andrew.nikulin@live.com)"
#property version   "999.666"
#property indicator_chart_window

#import "MetaQuotes.Client.IndicatorToMediator.dll"

int environment = 1; //Development = 1, Production = 4
int symbol;
string result;
bool connected = false;

int OnInit()
{
        if (Symbol() == "EURUSD") symbol = 1;
	else if (Symbol() == "GBPUSD") symbol = 2;
	else if (Symbol() == "USDJPY") symbol = 3;
	else if (Symbol() == "EURGBP") symbol = 4;
	else if (Symbol() == "EURJPY") symbol = 5;
	else if (Symbol() == "GBPJPY") symbol = 6;
	else { Print("This chart has a wrong Symbol."); PlaySound("disconnect.wav"); return INIT_FAILED; }
	
	MqlTick last_tick;
	if(SymbolInfoTick(Symbol(), last_tick))
	{
	   result = IndicatorToMediatorService::Init(symbol, TimeToString(last_tick.time, TIME_DATE | TIME_SECONDS), last_tick.ask, last_tick.bid, environment);
      if (result == "ok")
      {
         connected = true;
         Print(Symbol() + ":" + result);
         return(INIT_SUCCEEDED);
      }
      else
      {
         Print(result); 
         PlaySound("disconnect.wav"); 
         return INIT_SUCCEEDED;//if INIT_FAILED MT5 kicks off the indicator
      }
	}
	else 
	{
	   Print("SymbolInfoTick() failed, error = ", GetLastError());
	   return(INIT_SUCCEEDED);//if INIT_FAILED MT5 kicks off the indicator
	}
}

void OnTick()
{
   if(!connected) return;
   MqlTick last_tick;
   if(SymbolInfoTick(Symbol(), last_tick))
   {
      result = IndicatorToMediatorService::Tick(symbol, TimeToString(last_tick.time, TIME_DATE | TIME_SECONDS), last_tick.ask, last_tick.bid);
      if (result != "ok")
      {
         connected = false;
         Print(result); 
         PlaySound("disconnect.wav");
      }
   }
   else 
   {
      connected = false;
      Print("SymbolInfoTick() failed, error = ", GetLastError());
      PlaySound("disconnect.wav");
   }
}

void OnDeinit(const int reason)
{
   if(!connected) return;
   IndicatorToMediatorService::DeInit(symbol, reason);
}
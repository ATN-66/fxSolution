//+------------------------------------------------------------------+
//|                                                    Indicator.mq5 |
//|       Copyright © 2023, Andrew Nikulin (andrew.nikulin@live.com) |
//+------------------------------------------------------------------+
string indicatorShortName = "Indicator";
#property copyright "Copyright © 2023, Andrew Nikulin (andrew.nikulin@live.com)"
#property version   "999.666"
#property indicator_chart_window

#import "MetaQuotes.Client.Indicator.To.Mediator.dll"

int environment = 1; //Development = 1, Production = 4
int symbol;
string ok = "ok";
string result;
bool connected = false;
int id = 0;

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
{  
   while(!IsMarketOpen(Symbol()))
   {
      //Print("Market is closed, waiting...");
      Sleep(10000); // sleep for 10 seconds before checking again
   }
     
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
	   string sep = ":";
	   ushort u_sep = StringGetCharacter(sep, 0);
	   string output[];
	   string mediatorResponse;
	   mediatorResponse = Mediator::Init(id++, symbol, TimeToString(last_tick.time, TIME_DATE | TIME_SECONDS), last_tick.ask, last_tick.bid, environment);
	   Print(mediatorResponse);
	   int k = StringSplit(mediatorResponse, u_sep, output);
	   result = output[2];
	   Print(Symbol() + ":" + result + ", " + output[0] + ", " + output[1]);
	   if (result == ok)
      {
         connected = true;
         return(INIT_SUCCEEDED);
      }
      else
      {
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
      result = Mediator::Tick(id++, symbol, TimeToString(last_tick.time, TIME_DATE | TIME_SECONDS), last_tick.ask, last_tick.bid);     
      if (result != ok)
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
   Mediator::DeInit(reason);
}

bool IsMarketOpen(string symbolToCheck)
{
    // Variables
    datetime cur_time;
    MqlDateTime dt;
    datetime open_time, close_time;

    // Get current server time and day of week
    cur_time = TimeCurrent();
    TimeToStruct(cur_time, dt);
    ENUM_DAY_OF_WEEK dayOfWeek = ENUM_DAY_OF_WEEK(dt.day_of_week);

    // Get today's session open and close times
    if(!SymbolInfoSessionQuote(symbolToCheck, dayOfWeek, 0, open_time, close_time))
    {
        Print("Error in SymbolInfoSessionQuote for ", symbolToCheck, ", day ", dayOfWeek, ", session 0. Error code = ", GetLastError());
        return(false);
    }

    // Check if current time is within today's trading session
    if(cur_time >= open_time && cur_time <= close_time)
        return(true);

    // If current time is before open time, check previous day's session
    if(cur_time < open_time)
    {
        ENUM_DAY_OF_WEEK prevDayOfWeek = ENUM_DAY_OF_WEEK(dayOfWeek - 1);
        if(prevDayOfWeek < 0) prevDayOfWeek = 6;  // if current day is Sunday, previous day is Saturday

        // Get previous day's session open and close times
        if(!SymbolInfoSessionQuote(symbolToCheck, prevDayOfWeek, 0, open_time, close_time))
        {
            Print("Error in SymbolInfoSessionQuote for ", symbolToCheck, ", day ", prevDayOfWeek, ", session 0. Error code = ", GetLastError());
            return(false);
        }

        // Check if current time is within previous day's trading session
        if(cur_time >= open_time && cur_time <= close_time)
            return(true);
    }

    return(false);
}

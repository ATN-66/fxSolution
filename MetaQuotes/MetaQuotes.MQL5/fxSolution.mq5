//+------------------------------------------------------------------+
//|                                                   fxSolution.mq5 |
//|       Copyright © 2023, Andrew Nikulin (andrew.nikulin@live.com) |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2023, Andrew Nikulin (andrew.nikulin@live.com)"
#property link "andrew.nikulin@live.com"
#property version "1.00"
#property strict
#property description "Expert Advisor designed as a data and order execution hub. This EA connects to a proprietary DLL to receive market data and send trading signals. It automates trade execution based on received signals, managing both the opening and closing of orders for effective and systematic trading."
//+------------------------------------------------------------------+
//| Imports                                                          |
//+------------------------------------------------------------------+
#import "MetaQuotes.Client.dll"
//+------------------------------------------------------------------+
//| Inputs                                                           |
//+------------------------------------------------------------------+
string const OK = "ok";
int const ENVIRONMENT = 1; //Development = 1, Production = 4

int symbol;
string result;
bool connected = false;
int id = 0;

// Market opening and closing times in broker server time
int market_open_hour = 21;  // 9 PM broker server time on Sunday
int const MARKET_OPEN_MINUTE = 0;
int const MARKET_OPEN_DAY = 0;    // 0 for Sunday

int market_close_hour = 21; // 9 PM broker server time on Friday
int const MARKET_CLOSE_MINUTE = 0;
int const MARKET_CLOSE_DAY = 5;   // 5 for Friday
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit(){
   //--- create timer
   EventSetTimer(60);
   
   int DST_correction = TimeDaylightSavings();//todo: it looks like it is from local computer and not from broker
   if (DST_correction == 0){ //Print("Currently in Standard Time (Winter Time)");
   }
   else if (DST_correction == 3600){ //Print("Currently in Daylight Saving Time (Summer Time)");
   }

   while(!IsMarketOpen(Symbol())){
      Sleep(60000); // sleep for 1 minute before checking again
   }

   symbol = getSymbolCode(Symbol());
   if (symbol == -1){
      Print("This chart has a wrong Symbol."); 
      PlaySound("disconnect.wav"); 
      return INIT_FAILED; 
   }

   string mediatorResponse = initiateMediatorConnection(Symbol());
   if (mediatorResponse != OK)
   {
      Print(mediatorResponse);
      PlaySound("disconnect.wav"); 
      return INIT_SUCCEEDED;
   }

   connected = true;
   return INIT_SUCCEEDED;
}
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason){
   //--- destroy timer
   EventKillTimer();
   
   if(!connected) return;
   Mediator::DeInit(symbol, reason);
}
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick(){
   //---
   if(!connected) return;
   
   MqlTick last_tick;
   if(SymbolInfoTick(Symbol(), last_tick))
   {
      result = Mediator::Tick(id++, symbol, TimeToString(last_tick.time, TIME_DATE | TIME_SECONDS), last_tick.ask, last_tick.bid);     
      if (result != OK)
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
//+------------------------------------------------------------------+
//| Timer function                                                   |
//+------------------------------------------------------------------+
void OnTimer(){
   //---
   
}
//+------------------------------------------------------------------+
//| Trade function                                                   |
//+------------------------------------------------------------------+
void OnTrade(){
   //---
   
}
//+------------------------------------------------------------------+
//| TradeTransaction function                                        |
//+------------------------------------------------------------------+
void OnTradeTransaction(const MqlTradeTransaction& trans, const MqlTradeRequest& transRequest, const MqlTradeResult& transResult){
   //---
   
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int getSymbolCode(string symbolStr)
{
   if (symbolStr == "EURUSD") return 1;
   else if (symbolStr == "GBPUSD") return 2;
   else if (symbolStr == "USDJPY") return 3;
   else if (symbolStr == "EURGBP") return 4;
   else if (symbolStr == "EURJPY") return 5;
   else if (symbolStr == "GBPJPY") return 6;
   else return -1;
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
string initiateMediatorConnection(string symbolStr)
{
   MqlTick last_tick;
   if(!SymbolInfoTick(symbolStr, last_tick))
   {
      Print("SymbolInfoTick() failed, error = ", GetLastError());
      return "SymbolInfoTick failed";
   }

   string sep = ":";
   ushort u_sep = StringGetCharacter(sep, 0);
   string output[];
   string mediatorResponse = Mediator::Init(id++, symbol, TimeToString(last_tick.time, TIME_DATE | TIME_SECONDS), last_tick.ask, last_tick.bid, ENVIRONMENT);
   Print(mediatorResponse);
   int k = StringSplit(mediatorResponse, u_sep, output);
   return output[2];
}
//+------------------------------------------------------------------+
//| Check if market is open for the given symbol                     |
//+------------------------------------------------------------------+
bool IsMarketOpen(string symbolToCheck)
{
   datetime cur_time = TimeCurrent();
   MqlDateTime dt;
   TimeToStruct(cur_time, dt);

   // Check if market is open
   if(dt.day_of_week > MARKET_OPEN_DAY && dt.day_of_week < MARKET_CLOSE_DAY)
   {
      return ConfirmIfMarketIsOpen(symbolToCheck);
   }
   else if(dt.day_of_week == MARKET_OPEN_DAY && (dt.hour > market_open_hour || (dt.hour == market_open_hour && dt.min >= MARKET_OPEN_MINUTE)))
   {
      return ConfirmIfMarketIsOpen(symbolToCheck);
   }
   else if(dt.day_of_week == MARKET_CLOSE_DAY && (dt.hour < market_close_hour || (dt.hour == market_close_hour && dt.min < MARKET_CLOSE_MINUTE)))
   {
      return ConfirmIfMarketIsOpen(symbolToCheck);
   }
   // If none of the conditions are met, market is not open
   return false;
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool ConfirmIfMarketIsOpen(string symbolToCheck)
{
    MqlTick last_tick;
    if(!SymbolInfoTick(symbolToCheck, last_tick))
    {
        Print("Error in SymbolInfoTick. Error code = ", GetLastError());
        return(false);
    }

    datetime cur_time = TimeCurrent();
    if(cur_time - last_tick.time < 60) // replace 60 with your chosen threshold in seconds
        return(true);
    else
        return(false);
}
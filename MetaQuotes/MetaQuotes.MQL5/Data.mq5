//+------------------------------------------------------------------+
//|                                                         Data.mq5 |
//|       Copyright © 2023, Andrew Nikulin (andrew.nikulin@live.com) |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2023, Andrew Nikulin (andrew.nikulin@live.com)"
#property link "andrew.nikulin@live.com"
#property version "1.00"
#property strict
#property description "Expert Advisor designed as a data hub. This EA connects to a proprietary DLL to receive market data."
//+------------------------------------------------------------------+
//| Imports                                                          |
//+------------------------------------------------------------------+
#import "MetaQuotes.Data.dll"
//+------------------------------------------------------------------+
//| Inputs                                                           |
//+------------------------------------------------------------------+
input string Inp_Expert_Title = "Data";
string const OK = "ok";
int const WORKPLACE = 1; //Development = 1, Production = 4

int symbolCode;
string callResult;
bool connected = false;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit() {
   
   if(!CheckSymbolsPips()) {
      Print("This chart has a wrong pips/pipette setup."); 
      PlaySound("disconnect.wav"); 
      return INIT_FAILED; 
   }
     
   symbolCode = GetSymbolCode(Symbol());
   if (symbolCode == -1){
      Print("This chart has a wrong Symbol."); 
      PlaySound("disconnect.wav"); 
      return INIT_FAILED; 
   }

   callResult = InitiateMediatorConnection();
   if (callResult != OK)
   {
      Print(callResult);
      PlaySound("disconnect.wav"); 
      return INIT_SUCCEEDED;
   }

   connected = true;
   return INIT_SUCCEEDED;
}
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason) {
   if(!connected) return;
   DataMediator::DeInit(symbolCode, reason);
}
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick() { 
   if(!connected) return;
   MqlTick last_tick;
   if(SymbolInfoTick(Symbol(), last_tick))
   {
      callResult = DataMediator::Tick(symbolCode, TimeToString(last_tick.time, TIME_DATE | TIME_MINUTES | TIME_SECONDS), last_tick.ask, last_tick.bid);
      if (callResult != OK)
      {
         connected = false;
         Print(callResult); 
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
//| CheckSymbolsPips                                                 |
//+------------------------------------------------------------------+
bool CheckSymbolsPips() {
   string symbols[] = {"EURUSD", "GBPUSD", "USDJPY", "EURGBP", "EURJPY", "GBPJPY"};
   for (int i = 0; i < ArraySize(symbols); i++) {
      int digits = (int)SymbolInfoInteger(symbols[i], SYMBOL_DIGITS); 
      if (digits == 4 || digits == 2) {
         Print("The platform uses pips for ", symbols[i]);
         return false;
      } else if (digits == 5 || digits == 3) {
         continue;
      } else {
         Print("Unknown pip/pipette setting for ", symbols[i]);
      }
   }
   return true;
}
//+------------------------------------------------------------------+
//| GetSymbolCode                                                    |
//+------------------------------------------------------------------+
int GetSymbolCode(string symbolStr) {
        if (symbolStr == "EURUSD") return 1;
   else if (symbolStr == "GBPUSD") return 2;
   else if (symbolStr == "USDJPY") return 3;
   else if (symbolStr == "EURGBP") return 4;
   else if (symbolStr == "EURJPY") return 5;
   else if (symbolStr == "GBPJPY") return 6;
   else return -1;
}
//+------------------------------------------------------------------+
//| GetSymbolStr                                                     |
//+------------------------------------------------------------------+
string GetSymbolStr(int code) {
    switch(code) {
        case 1: return "EURUSD";
        case 2: return "GBPUSD";
        case 3: return "USDJPY";
        case 4: return "EURGBP";
        case 5: return "EURJPY";
        case 6: return "GBPJPY";
        default: return "";
    }
}
//+------------------------------------------------------------------+
//| InitiateMediatorConnection                                       |
//+------------------------------------------------------------------+
string InitiateMediatorConnection()
{
   string symbolStr = GetSymbolStr(symbolCode);
   if(symbolStr == "") {
      Print("Invalid symbol code"); 
      return "Invalid symbol code"; 
   }

   MqlTick last_tick;
   if(!SymbolInfoTick(symbolStr, last_tick))
   {
      Print("SymbolInfoTick() failed, error = ", GetLastError());
      return "SymbolInfoTick failed";
   }

   string sep = ":";
   ushort u_sep = StringGetCharacter(sep, 0);
   string output[];
   string mediatorResponse = DataMediator::Init(symbolCode, TimeToString(last_tick.time, TIME_DATE | TIME_MINUTES | TIME_SECONDS), last_tick.ask, last_tick.bid, WORKPLACE);
   Print(mediatorResponse);
   int k = StringSplit(mediatorResponse, u_sep, output);
   return output[2];
}
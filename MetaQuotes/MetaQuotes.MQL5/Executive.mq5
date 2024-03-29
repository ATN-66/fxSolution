//+------------------------------------------------------------------+
//|                                                    Executive.mq5 |
//|       Copyright © 2023, Andrew Nikulin (andrew.nikulin@live.com) |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2023, Andrew Nikulin (andrew.nikulin@live.com)"
#property link "andrew.nikulin@live.com"
#property version "1.00"
#property strict
#property description "Expert Advisor designed as order execution hub. This EA connects to a proprietary DLL to send trading signals. It automates trade execution based on received signals, managing both the opening and closing of orders for effective and systematic trading."
//+------------------------------------------------------------------+
//| Includes                                                         |
//+------------------------------------------------------------------+
#include <Trade\AccountInfo.mqh>
#include <Trade\SymbolInfo.mqh>
#include <Trade\Trade.mqh>
CTrade trade;
CSymbolInfo symbol_info;
CAccountInfo account;
//+------------------------------------------------------------------+
//| Imports                                                          |
//+------------------------------------------------------------------+
#import "MetaQuotes.Executive.dll" 
//+------------------------------------------------------------------+
//| Inputs                                                           |
//+------------------------------------------------------------------+
input string Inp_Expert_Title = "Executive";
input int const WORKPLACE = 4; //Development = 1, Production = 4

string symbols[] = {"EURUSD", "GBPUSD", "USDJPY", "EURGBP", "EURJPY", "GBPJPY"};
string const OK = "ok";
bool connected = false;

string Ticket = "";
string transactionDetails = "";
ulong positionTicket = -1;
bool positionOpen = false;
ulong deviation = -1;

string callResult;
struct ResultStruct {
    string result;
    string code;
};
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit() {

   if(!CheckSymbolsPips()) {
      Print("This chart has a wrong pips/pipette setup."); 
      PlaySound("disconnect.wav"); 
      return INIT_FAILED; 
   }

   string initResult = ExecutiveMediator::Init(TimeToString(TimeCurrent(), TIME_DATE | TIME_MINUTES | TIME_SECONDS));
   bool status = CheckInitStatus(initResult);
   if (!status) {
      PlaySound("disconnect.wav"); 
      return INIT_FAILED;
   }

   trade.SetAsyncMode(true); 
   EventSetTimer(1);
   connected = true;
   return INIT_SUCCEEDED;
}
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason) {
//todo:
   //if(!connected) return;
   
   //ExecutiveMediator::DeInit(TimeToString(TimeCurrent(), TIME_DATE | TIME_MINUTES | TIME_SECONDS));          
   //EventKillTimer();//--- destroy timer
}
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick(){
   // ignore
}
//+------------------------------------------------------------------+
//| Timer function                                                   |
//+------------------------------------------------------------------+
void OnTimer(){
   callResult = ExecutiveMediator::Pulse(TimeToString(TimeCurrent(), TIME_DATE | TIME_MINUTES | TIME_SECONDS), "0", "0", "0", "0", "0");
   if(callResult != OK)
   {
      //Print(callResult);
      string Type;
      string Code;
      string Details;
      string parts[];
      if (StringSplit(callResult, ',', parts)) {
         for (int i = 0; i < ArraySize(parts); i++) {
            string key_value[];
            StringTrimRight(parts[i]);
            StringTrimLeft(parts[i]);
            if (StringSplit(parts[i], ':', key_value)) {
               if (key_value[0] == "Type") {
                  StringTrimRight(key_value[1]);
                  StringTrimLeft(key_value[1]);
                  Type = key_value[1];
               } else if (key_value[0] == "Code") {
                  StringTrimRight(key_value[1]);
                  StringTrimLeft(key_value[1]);
                  Code = key_value[1];
               } else if (key_value[0] == "Ticket") {
                  StringTrimRight(key_value[1]);
                  StringTrimLeft(key_value[1]);
                  Ticket = key_value[1];
               }
               else if (key_value[0] == "Details") {
                  StringTrimRight(key_value[1]);
                  StringTrimLeft(key_value[1]);
                  Details = key_value[1];
                  // optional, to remove the curly braces:
                  Details = StringSubstr(Details, 1, StringLen(Details) - 2);
               }
            }
         }
      }
      
      //Print("Type:", Type, ", Code:", Code, ", Ticket:", Ticket, ", Details:", Details); //
      if(Type == "AccountInfo") {
         if(Code == "AccountProperties") {
            ResultStruct resultStruct = AccountProperties();
            callResult = ExecutiveMediator::AccountProperties(TimeToString(TimeCurrent(), TIME_DATE | TIME_MINUTES | TIME_SECONDS), Type, Code, Ticket, resultStruct.code, resultStruct.result);
            //Print("callResult:", callResult); // callResult:ok
            if (callResult != OK) { PlaySound("alert2.wav"); }
         } else if(Code == "TradingHistory") {
            ResultStruct resultStruct = TradingHistory(Details);
            callResult = ExecutiveMediator::TradingHistory(TimeToString(TimeCurrent(), TIME_DATE | TIME_MINUTES | TIME_SECONDS), Type, Code, Ticket, resultStruct.code, resultStruct.result);
            //Print("callResult:", callResult); // callResult:ok
            if (callResult != OK) { PlaySound("alert2.wav"); }
         } else if(Code == "TickValues") {
            ResultStruct resultStruct = TickValues();
            callResult = ExecutiveMediator::TickValues(TimeToString(TimeCurrent(), TIME_DATE | TIME_MINUTES | TIME_SECONDS), Type, Code, Ticket, resultStruct.code, resultStruct.result);
            //Print("callResult:", callResult); // callResult:ok
            if (callResult != OK) { PlaySound("alert2.wav"); }
         } else {
            Print("Code is NOT PROCESSED: ", Code);
            PlaySound("alert2.wav");
            return;
         } 
      }
      else if(Type == "TradeCommand") {
         if(Code == "OpenPosition") {
            ResultStruct resultStruct = OpenPosition(Details);
            callResult = ExecutiveMediator::UpdatePosition(TimeToString(TimeCurrent(), TIME_DATE | TIME_MINUTES | TIME_SECONDS), Type, Code, Ticket, resultStruct.code, resultStruct.result);
            //Print("callResult:", callResult); // callResult:ok
            if (callResult != OK) { PlaySound("alert2.wav"); }
         } else if(Code == "ClosePosition") {
            ResultStruct resultStruct = ClosePosition(Details);
            callResult = ExecutiveMediator::UpdatePosition(TimeToString(TimeCurrent(), TIME_DATE | TIME_MINUTES | TIME_SECONDS), Type, Code, Ticket, resultStruct.code, resultStruct.result);
            //Print("callResult:", callResult); // callResult:ok
            if (callResult != OK) { PlaySound("alert2.wav"); }
         } else if(Code == "ModifyPosition") {
            ResultStruct resultStruct = ModifyPosition(Details);
            callResult = ExecutiveMediator::UpdatePosition(TimeToString(TimeCurrent(), TIME_DATE | TIME_MINUTES | TIME_SECONDS), Type, Code, Ticket, resultStruct.code, resultStruct.result);
            //Print("callResult:", callResult); // callResult:ok
            if (callResult != OK) { PlaySound("alert2.wav"); }
         } else {
            Print("Code is NOT PROCESSED: ", Code);
            PlaySound("alert2.wav");
            return;
         }
      }
      else {
         Print("Type is NOT PROCESSED: ", Type);
         PlaySound("alert2.wav");
         return;
      }
   }
}
//+------------------------------------------------------------------+
//| Trade function                                                   |
//+------------------------------------------------------------------+
void OnTrade() {
   // ignore
}
//+------------------------------------------------------------------+
//| CheckSymbolsPips                                                 |
//+------------------------------------------------------------------+
bool CheckSymbolsPips() {
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
//| CheckInitStatus                                                  |
//+------------------------------------------------------------------+
bool CheckInitStatus(string result) {
    string parts[];
    int length = StringSplit(result, ':', parts);
    if(length >= 3) { // we expect at least 3 parts
        string entity = parts[0];
        string guid = parts[1];
        string status = parts[2];

        if(status == OK) {
            // Call succeeded
            Print("Init succeeded for entity: ", entity, " with GUID: ", guid);
            return true;
        } else {
            // Call failed
            Print("Init failed for entity: ", entity, " with GUID: ", guid, ". Status: ", status);
            return false;
        }
    } else {
        Print("Invalid init result:");
        Print(result);
        return false;
    }
}
//+------------------------------------------------------------------+
//| ModifyPosition                                                   |
//+------------------------------------------------------------------+
ResultStruct ModifyPosition(string details) {
   //Print(__FUNCTION__, " --> ",  details);
   ResultStruct resultStruct;
   string detailsParts[]; 
   int ticket = -1;
   string symbol = "";
   double stopLoss = -1;
   double takeProfit = -1;
   double allowedStopLoss = 0;
   double allowedTakeProfit = 0;
   ENUM_POSITION_TYPE positionType = -1;
   double point = 0;
   
   StringSplit(details, ';', detailsParts);
   for (int i = 0; i < ArraySize(detailsParts); i++) {
      string key_value[];
      if (StringSplit(detailsParts[i], '>', key_value)) {
         StringTrimRight(key_value[1]);
         StringTrimLeft(key_value[1]);
         if (key_value[0] == "ticket") {
            ticket = (int)StringToInteger(key_value[1]);
            //Print(__FUNCTION__, " --> ", "ticket:", ticket); 
         } else if (key_value[0] == "symbol") {
            symbol = key_value[1];
            //Print(__FUNCTION__, " --> ", "symbol:", symbol);
         } else if (key_value[0] == "stopLoss") {
            stopLoss = StringToDouble(key_value[1]);
            //Print(__FUNCTION__, " --> ", "stopLoss:", stopLoss);
         } else if (key_value[0] == "takeProfit") {
            takeProfit = StringToDouble(key_value[1]);
            //Print(__FUNCTION__, " --> ", "takeProfit:", takeProfit); 
         } else {
            PlaySound("alert2.wav");
            Print("key_value[0] is NOT PROCESSED: ", key_value[0]);
            resultStruct.code = "FAILURE";
            resultStruct.result = "65537";
            return resultStruct;
         }
      }
   }
   
   stopLoss = NormalizeDouble(stopLoss, (int)SymbolInfoInteger(symbol, SYMBOL_DIGITS));
   takeProfit = NormalizeDouble(takeProfit, (int)SymbolInfoInteger(symbol, SYMBOL_DIGITS));
   //Print("Requested stopLoss: ", stopLoss, ", Requested takeProfit: ", takeProfit);
   
   double bid = 0.0;
   double ask = 0.0;
   MqlTick mqlTick;
   if(SymbolInfoTick(symbol, mqlTick)) {
      bid = mqlTick.bid;
      ask = mqlTick.ask;
   }

   if(PositionSelect(symbol)) {
      positionType = (ENUM_POSITION_TYPE)PositionGetInteger(POSITION_TYPE);
      if(positionType != POSITION_TYPE_BUY && positionType != POSITION_TYPE_SELL) {
         int err = GetLastError();
         ResetLastError();
         PlaySound("alert2.wav");
         Print("positionType != POSITION_TYPE_BUY && positionType != POSITION_TYPE_SELL:");
         resultStruct.code = "FAILURE";
         resultStruct.result = ErrorDescription(err);
         return resultStruct;
      }
   } else {
      Print("Failed to select position with ticket ", ticket);
      int err = GetLastError();
      ResetLastError();
      PlaySound("alert2.wav");
      Print("PositionGetTicket failed with error: ", err, ": ", ErrorDescription(err), ", WORKPLACE: ", WORKPLACE);
      resultStruct.code = "FAILURE";
      resultStruct.result = ErrorDescription(err);
      return resultStruct;
   }

   long stopsLevel = SymbolInfoInteger(symbol, SYMBOL_TRADE_STOPS_LEVEL);
   //Print("stopsLevel:", stopsLevel);
   point = SymbolInfoDouble(symbol, SYMBOL_POINT);
   //Print("point:", point);
   
   if (positionType == POSITION_TYPE_BUY) { 
      allowedStopLoss = NormalizeDouble(bid - stopsLevel * point, (int)SymbolInfoInteger(symbol, SYMBOL_DIGITS));
      stopLoss = MathMin(stopLoss, allowedStopLoss);
      allowedTakeProfit = NormalizeDouble(bid + stopsLevel * point, (int)SymbolInfoInteger(symbol, SYMBOL_DIGITS));
      takeProfit = MathMax(takeProfit, allowedTakeProfit);   
   } else if (positionType == POSITION_TYPE_SELL) { 
      allowedStopLoss = NormalizeDouble(ask + stopsLevel * point, (int)SymbolInfoInteger(symbol, SYMBOL_DIGITS));
      stopLoss = MathMax(stopLoss, allowedStopLoss);
      allowedTakeProfit = NormalizeDouble(ask - stopsLevel * point, (int)SymbolInfoInteger(symbol, SYMBOL_DIGITS));
      takeProfit = MathMin(takeProfit, allowedTakeProfit);
   }
   
   //Print("Allowed stopLoss: ", allowedStopLoss, ", Allowed takeProfit: ", allowedTakeProfit);
   //Print("Adjusted stopLoss: ", stopLoss, ", Adjusted takeProfit: ", takeProfit);
   
   double sl = NormalizeDouble(PositionGetDouble(POSITION_SL), (int)SymbolInfoInteger(symbol, SYMBOL_DIGITS));
   double tp = NormalizeDouble(PositionGetDouble(POSITION_TP), (int)SymbolInfoInteger(symbol, SYMBOL_DIGITS));
   
   if (sl == stopLoss && tp == takeProfit) {
      Print("sl == stopLoss && tp == takeProfit");
   }
   
   bool pm = trade.PositionModify(ticket, stopLoss, takeProfit);
   if (!pm) {
      int err = GetLastError();
      ResetLastError();
      PlaySound("alert2.wav");
      Print("OrderSend failed with error: ", err, ": ", ErrorDescription(err), ", WORKPLACE: ", WORKPLACE);
      resultStruct.code = "FAILURE";
      resultStruct.result = ErrorDescription(err);
      return resultStruct;
   } else {
      //PlaySound("ok.wav");
      resultStruct.code = "SUCCESS";
      resultStruct.result = "See update...";
      return resultStruct;
   }
}
//+------------------------------------------------------------------+
//| OpenPosition                                                     |
//+------------------------------------------------------------------+
ResultStruct OpenPosition(string details) {
   Print(__FUNCTION__, " --> ",  details); // Symbol>EURUSD;TradeType>Sell;Deviation>10;FreeMarginPercentToUse>90;FreeMarginPercentToRisk>5;MagicNumber>7077;Comment>Order opened by EXECUTIVE EA
   ResultStruct resultStruct;
   string detailsParts[]; 
   
   string symbol; // in details
   ENUM_ORDER_TYPE tradeType = -1; // in details
   double freeMarginPercentToUse = -1; // in details
   double freeMarginPercentToRisk = -1; // in details
   ulong magicNumber = -1; // in details
   string orderComment = ""; // in details
   double stopLossInPipettes = -1; // calculated
   double takeProfitInPipettes = -1; // calculated
   double volume = -1; // calculated
   double price = -1; // calculated
   double stopLoss = -1; // calculated
   double takeProfit = -1; // calculated
   
   StringSplit(details, ';', detailsParts);
   for (int i = 0; i < ArraySize(detailsParts); i++) {
      string key_value[];
      if (StringSplit(detailsParts[i], '>', key_value)) {
         StringTrimRight(key_value[1]);
         StringTrimLeft(key_value[1]);
         if (key_value[0] == "symbol") {
            symbol = key_value[1];
            //Print(__FUNCTION__, " --> ", "symbol:", symbol); 
         } else if (key_value[0] == "tradetype") {
            if (key_value[1] == "Sell") {
               tradeType = ORDER_TYPE_SELL;
            } else if (key_value[1] == "Buy") {
               tradeType = ORDER_TYPE_BUY;
            } else {
               PlaySound("alert2.wav");
               Print("tradeType is NOT PROCESSED: ", tradeType);
               resultStruct.code = "FAILURE";
               resultStruct.result = "65537";
               return resultStruct; 
            }
            //Print(__FUNCTION__, " --> ", "tradeType:", tradeType); 
         } else if (key_value[0] == "deviation") {
            deviation = StringToInteger(key_value[1]);
            //Print(__FUNCTION__, " --> ", "deviation:", deviation);
         } else if (key_value[0] == "freeMarginPercentToUse") {
            freeMarginPercentToUse = StringToDouble(key_value[1]);
            //Print(__FUNCTION__, " --> ", "freeMarginPercentToUse:", freeMarginPercentToUse);
         } else if (key_value[0] == "freeMarginPercentToRisk") {
            freeMarginPercentToRisk = StringToDouble(key_value[1]);
            //Print(__FUNCTION__, " --> ", "freeMarginPercentToRisk:", freeMarginPercentToRisk); 
         } else if (key_value[0] == "magicNumber") {
            magicNumber = StringToInteger(key_value[1]);
            //Print(__FUNCTION__, " --> ", "magicNumber:", magicNumber);  
         } else if (key_value[0] == "comment") {
            orderComment = key_value[1];
            //Print(__FUNCTION__, " --> ", "orderComment:", orderComment);  
         } else {
            PlaySound("alert2.wav");
            Print("key_value[0] is NOT PROCESSED: ", key_value[0]);
            resultStruct.code = "FAILURE";
            resultStruct.result = "65537";
            return resultStruct;
         }
      }
   }
      
   if (tradeType == ORDER_TYPE_BUY) {
      price = NormalizeDouble(SymbolInfoDouble(symbol, SYMBOL_ASK), (int)SymbolInfoInteger(symbol, SYMBOL_DIGITS));
   } else if (tradeType == ORDER_TYPE_SELL) {
      price = NormalizeDouble(SymbolInfoDouble(symbol, SYMBOL_BID), (int)SymbolInfoInteger(symbol, SYMBOL_DIGITS));
   } else {
      PlaySound("alert2.wav");
      Print("tradeType is NOT PROCESSED: ", tradeType);
      resultStruct.code = "FAILURE";
      resultStruct.result = "65537";
      return resultStruct;
   }
   
   volume = MaxLotCheckAndValidate(symbol, tradeType, price, freeMarginPercentToUse);
   if (volume == -1.0){
       PlaySound("alert2.wav");
       resultStruct.code = "FAILURE";
       resultStruct.result = "function MaxLotCheckAndValidate returned -1.0";
      return resultStruct;      
   }
   
   Print(__FUNCTION__, " --> ", "volume:", volume);
   double pipetteValuePerLotInAccountCurrency = NormalizeDouble(SymbolInfoDouble(symbol, SYMBOL_TRADE_TICK_VALUE), (int)SymbolInfoInteger(symbol, SYMBOL_DIGITS));
   Print(__FUNCTION__, " --> ", "pipetteValuePerLotInAccountCurrency:", pipetteValuePerLotInAccountCurrency);
   double equityToUse = AccountInfoDouble(ACCOUNT_EQUITY) / 100 * freeMarginPercentToUse;
   Print(__FUNCTION__, " --> ", "equityToUse:", equityToUse);
   double riskMoney = NormalizeDouble(equityToUse / 100 * freeMarginPercentToRisk, 2);
   Print(__FUNCTION__, " --> ", "riskMoney:", riskMoney);
   stopLossInPipettes = NormalizeDouble(riskMoney / (pipetteValuePerLotInAccountCurrency * volume), 0);
   takeProfitInPipettes = stopLossInPipettes;
   Print(__FUNCTION__, " --> ", "stopLossInPipettes:", stopLossInPipettes, ", takeProfitInPipettes:", takeProfitInPipettes);
   if (tradeType == ORDER_TYPE_BUY) {
      stopLoss = NormalizeDouble(price - stopLossInPipettes * SymbolInfoDouble(symbol, SYMBOL_POINT), (int)SymbolInfoInteger(symbol, SYMBOL_DIGITS));
      takeProfit = NormalizeDouble(price + stopLossInPipettes * SymbolInfoDouble(symbol, SYMBOL_POINT), (int)SymbolInfoInteger(symbol, SYMBOL_DIGITS));
   } else if (tradeType == ORDER_TYPE_SELL) {
      stopLoss = NormalizeDouble(price + stopLossInPipettes * SymbolInfoDouble(symbol, SYMBOL_POINT), (int)SymbolInfoInteger(symbol, SYMBOL_DIGITS));
      takeProfit = NormalizeDouble(price - stopLossInPipettes * SymbolInfoDouble(symbol, SYMBOL_POINT), (int)SymbolInfoInteger(symbol, SYMBOL_DIGITS));
   } else {
      PlaySound("alert2.wav");
      Print("tradeType is NOT PROCESSED: ", tradeType);
      resultStruct.code = "FAILURE";
      resultStruct.result = "65537";
      return resultStruct;
   }
   Print(__FUNCTION__, " --> ", "stopLoss:", stopLoss, ", takeProfit:", takeProfit);
   
   trade.SetExpertMagicNumber(magicNumber); 
   trade.SetDeviationInPoints(deviation);
   Print(__FUNCTION__, " --> ", "symbol:", symbol, ", tradeType:", tradeType, ", volume:", volume, ", price:", price, ", stopLoss:", stopLoss, ", takeProfit:", takeProfit, ", orderComment:", orderComment);
   bool tpo = false;
   tpo = trade.PositionOpen(symbol, tradeType, volume, price, stopLoss, takeProfit, orderComment); // comment to cancel execution!
   if (!tpo){
      int err = GetLastError();
      ResetLastError();
      PlaySound("alert2.wav");
      Print("OrderSend failed with error: ", err, ": ", ErrorDescription(err), ", WORKPLACE: ", WORKPLACE);
      resultStruct.code = "FAILURE";
      resultStruct.result = ErrorDescription(err);
      return resultStruct;
   } else {
      PlaySound("ok.wav");
      resultStruct.code = "SUCCESS";
      resultStruct.result = "See update...";
      return resultStruct;
   }
}
//+------------------------------------------------------------------+
//| ClosePosition                                                     |
//+------------------------------------------------------------------+
ResultStruct ClosePosition(string details) {
   //Print(__FUNCTION__, " --> ",  details);
   ulong ticket = -1;
   ulong deviation = -1;
   ResultStruct resultStruct;
   string detailsParts[]; 
   StringSplit(details, ';', detailsParts);
   for (int i = 0; i < ArraySize(detailsParts); i++) {
      string key_value[];
      if (StringSplit(detailsParts[i], '>', key_value)) {
         StringTrimRight(key_value[1]);
         StringTrimLeft(key_value[1]);
         if (key_value[0] == "ticket") {
            ticket = StringToInteger(key_value[1]);
         } else if (key_value[0] == "deviation") {
            deviation = StringToInteger(key_value[1]);
         } else {
            PlaySound("alert2.wav");
            Print("key_value[0] is NOT PROCESSED: ", key_value[0]);
            resultStruct.code = "FAILURE";
            resultStruct.result = "65537";
            return resultStruct;
         }
      }
   }
   //Print(__FUNCTION__, " --> ", "ticket:", ticket, ", deviation:", deviation);
   bool pc = trade.PositionClose(ticket, deviation);
   if (!pc) {
      int err = GetLastError();
      ResetLastError();
      PlaySound("alert2.wav");
      Print("OrderSend failed with error: ", err, ": ", ErrorDescription(err), ", WORKPLACE: ", WORKPLACE);
      resultStruct.code = "FAILURE";
      resultStruct.result = ErrorDescription(err);
      return resultStruct;
   } else {
      PlaySound("ok.wav");
      resultStruct.code = "SUCCESS";
      resultStruct.result = "See update...";
      return resultStruct;
   }
}
//+------------------------------------------------------------------+
//| ErrorDescription                                                 |
//+------------------------------------------------------------------+
string ErrorDescription(int err_code) {
   switch(err_code) {
      case TRADE_RETCODE_INVALID: return "Invalid request";
      case TRADE_RETCODE_MARKET_CLOSED: return "Market is closed";
      case TRADE_RETCODE_INVALID_VOLUME: return "Invalid volume in the request";
      case TRADE_RETCODE_INVALID_PRICE: return "Invalid price in the request";
      case ERR_TRADE_SEND_FAILED: return "Trade request sending failed";
      case ERR_TRADE_POSITION_NOT_FOUND: return "Position not found";
      default: return "Unknown error. Code: " + IntegerToString(err_code);
   }
}
//+------------------------------------------------------------------+
//| SuccessDescription                                               |
//+------------------------------------------------------------------+
string SuccessDescription(int ret_code) {
    switch(ret_code) {
        case TRADE_RETCODE_DONE: return "Request completed";
        default: return "Unknown return. Code: " + IntegerToString(ret_code);
    }
}
//+------------------------------------------------------------------+ 
//| convert numeric response codes to string mnemonics               | 
//+------------------------------------------------------------------+ 
string GetRetcodeID(int retcode) 
  { 
   switch(retcode) 
     { 
      case 10004: return("TRADE_RETCODE_REQUOTE");             break; 
      case 10006: return("TRADE_RETCODE_REJECT");              break; 
      case 10007: return("TRADE_RETCODE_CANCEL");              break; 
      case 10008: return("TRADE_RETCODE_PLACED");              break; 
      case 10009: return("TRADE_RETCODE_DONE");                break; 
      case 10010: return("TRADE_RETCODE_DONE_PARTIAL");        break; 
      case 10011: return("TRADE_RETCODE_ERROR");               break; 
      case 10012: return("TRADE_RETCODE_TIMEOUT");             break; 
      case 10013: return("TRADE_RETCODE_INVALID");             break; 
      case 10014: return("TRADE_RETCODE_INVALID_VOLUME");      break; 
      case 10015: return("TRADE_RETCODE_INVALID_PRICE");       break; 
      case 10016: return("TRADE_RETCODE_INVALID_STOPS");       break; 
      case 10017: return("TRADE_RETCODE_TRADE_DISABLED");      break; 
      case 10018: return("TRADE_RETCODE_MARKET_CLOSED");       break; 
      case 10019: return("TRADE_RETCODE_NO_MONEY");            break; 
      case 10020: return("TRADE_RETCODE_PRICE_CHANGED");       break; 
      case 10021: return("TRADE_RETCODE_PRICE_OFF");           break; 
      case 10022: return("TRADE_RETCODE_INVALID_EXPIRATION");  break; 
      case 10023: return("TRADE_RETCODE_ORDER_CHANGED");       break; 
      case 10024: return("TRADE_RETCODE_TOO_MANY_REQUESTS");   break; 
      case 10025: return("TRADE_RETCODE_NO_CHANGES");          break; 
      case 10026: return("TRADE_RETCODE_SERVER_DISABLES_AT");  break; 
      case 10027: return("TRADE_RETCODE_CLIENT_DISABLES_AT");  break; 
      case 10028: return("TRADE_RETCODE_LOCKED");              break; 
      case 10029: return("TRADE_RETCODE_FROZEN");              break; 
      case 10030: return("TRADE_RETCODE_INVALID_FILL");        break; 
      case 10031: return("TRADE_RETCODE_CONNECTION");          break; 
      case 10032: return("TRADE_RETCODE_ONLY_REAL");           break; 
      case 10033: return("TRADE_RETCODE_LIMIT_ORDERS");        break; 
      case 10034: return("TRADE_RETCODE_LIMIT_VOLUME");        break; 
      case 10035: return("TRADE_RETCODE_INVALID_ORDER");       break; 
      case 10036: return("TRADE_RETCODE_POSITION_CLOSED");     break; 
      default: return("TRADE_RETCODE_UNKNOWN="+IntegerToString(retcode)); break;
     } 
 }
//+------------------------------------------------------------------+
//| MaxLotCheckAndValidate                                           |
//+------------------------------------------------------------------+
double MaxLotCheckAndValidate(string symbol, ENUM_ORDER_TYPE trade_operation, double price, double percent) {
   if(symbol=="" || price<=0.0 || percent<1 || percent>100) {
     Print("Invalid parameters");
     return(-1.0);
   }

   double margin = 0.0;
   if(!OrderCalcMargin(trade_operation, symbol, 1.0, price, margin) || margin < 0.0) {
     Print("Margin calculation failed");
     return(-1.0);
   }

   if(margin == 0.0) return(SymbolInfoDouble(symbol, SYMBOL_VOLUME_MAX));

   double volume = NormalizeDouble(AccountInfoDouble(ACCOUNT_MARGIN_FREE) * percent / 100.0 / margin, 2);
   double stepvol = SymbolInfoDouble(symbol, SYMBOL_VOLUME_STEP);
   if(stepvol > 0.0) volume = stepvol * MathFloor(volume / stepvol);

   double minvol = SymbolInfoDouble(symbol, SYMBOL_VOLUME_MIN);
   if(volume < minvol) {
      Print("Volume is less than minimum volume");
      volume = 0.0;
   }

   double maxvol = SymbolInfoDouble(symbol, SYMBOL_VOLUME_MAX);
   if(volume > maxvol) {
      Print("Volume is more than maximum volume");
      volume = maxvol;
   }

   MqlTradeRequest request;
   MqlTradeCheckResult result;

   request.action = TRADE_ACTION_DEAL; // Immediate execution
   request.symbol = symbol;
   request.volume = volume;
   request.type   = trade_operation;
   request.type_filling = ORDER_FILLING_FOK;
   request.price  = price;
   request.deviation = deviation; // Acceptable deviation in points
   
   if(!OrderCheck(request, result)) {
      int err = GetLastError(); // 10013 - TRADE_RETCODE_INVALID (Invalid request) // 10030 - TRADE_RETCODE_INVALID_FILL (Invalid order filling type)
      ResetLastError();
      //PlaySound("alert2.wav");
      Print(__FUNCTION__, " --> ", "OrderCheck failed with error: ", err, ": ", ErrorDescription(err), ", WORKPLACE: ", WORKPLACE);
      Print(__FUNCTION__, " --> ", "retcode:", result.retcode, ", margin_free:", result.margin_free, ", margin_level:", result.margin_level);
   } 
   return volume;
}
//+------------------------------------------------------------------+
//| AccountProperties                                                |
//+------------------------------------------------------------------+
ResultStruct AccountProperties() {
   ResultStruct resultStruct;
   if(WORKPLACE == 4) {
      string accountProperties = 
      "Login: " + IntegerToString(account.Login()) + 
      ", TradeMode: " + IntegerToString(account.TradeMode()) + 
      ", Leverage: " + IntegerToString(account.Leverage()) + 
      ", StopOutMode: " + IntegerToString(account.StopoutMode()) + 
      ", MarginMode: " + IntegerToString(account.MarginMode()) + 
      ", TradeAllowed: " + (account.TradeAllowed() ? "true" : "false") + 
      ", TradeExpert: " + (account.TradeExpert() ? "true" : "false") + 
      ", LimitOrders: " + IntegerToString(account.LimitOrders()) +
      ", Balance: " + DoubleToString(account.Balance(), 2) +
      ", Credit: " + DoubleToString(account.Credit(), 2) +
      ", Profit: " + DoubleToString(account.Profit(), 2) +
      ", Equity: " + DoubleToString(account.Equity(), 2) +
      ", Margin: " + DoubleToString(account.Margin(), 2) +
      ", FreeMargin: " + DoubleToString(account.FreeMargin(), 2) +
      ", MarginLevel: " + DoubleToString(account.MarginLevel(), 2) +
      ", MarginCall: " + DoubleToString(account.MarginCall(), 2) +
      ", MarginStopOut: " + DoubleToString(account.MarginStopOut(), 2) +
      ", Name: " + account.Name() + 
      ", Server: " + account.Server() + 
      ", Currency: " + account.Currency() + 
      ", Company: " + account.Company();
      resultStruct.code = "SUCCESS";
      resultStruct.result = accountProperties;
      return resultStruct;
   } else if (WORKPLACE == 1) {
      string accountProperties = 
      "Login: " + "101266616" + 
      ", TradeMode: " + "0" + 
      ", Leverage: " + "400" + 
      ", StopOutMode: " + "0" + 
      ", MarginMode: " + "2" + 
      ", TradeAllowed: " + "true" + 
      ", TradeExpert: " + "true" + 
      ", LimitOrders: " + "0" +
      ", Balance: " + "10000.00" +
      ", Credit: " + "0.00" +
      ", Profit: " + "0.00" +
      ", Equity: " + "10000.00" +
      ", Margin: " + "0.00" +
      ", FreeMargin: " + "10000.00" +
      ", MarginLevel: " + "0.00" +
      ", MarginCall: " + "50.00" +
      ", MarginStopOut: " + "10.00" +
      ", Name: " + "Andrew Nikulin" + 
      ", Server: " + "Ava-Demo 1-MT5" + 
      ", Currency: " + "CAD" + 
      ", Company: " + "Friedberg Powered by AvaTrade";
      resultStruct.code = "SUCCESS";
      resultStruct.result = accountProperties;
      return resultStruct;
   } else {
      PlaySound("alert2.wav");
      Print("WORKPLACE is wrong.");
      resultStruct.code = "FAILURE";
      resultStruct.result = "WORKPLACE is wrong.";
      return resultStruct;    
   }
}
//+------------------------------------------------------------------+
//| GetTickValue                                                     |
//+------------------------------------------------------------------+
double GetTickValue(string symbol) {
    double tickValue = SymbolInfoDouble(symbol, SYMBOL_TRADE_TICK_VALUE);
    int digits = (int)SymbolInfoInteger(symbol, SYMBOL_DIGITS);
    return NormalizeDouble(tickValue, digits);
}
//+------------------------------------------------------------------+
//| TickValues                                                       |
//+------------------------------------------------------------------+
ResultStruct TickValues() {
   ResultStruct resultStruct;
   string tickValues;
   for (int i = 0; i < ArraySize(symbols); i++) {
      double tickValue = GetTickValue(symbols[i]);
      tickValues += symbols[i] + ":" + DoubleToString(tickValue);
      if(i != ArraySize(symbols) - 1) {
         tickValues += ", ";
      }
   }
   Print(tickValues);
   resultStruct.code = "SUCCESS";
   resultStruct.result = tickValues;
   return resultStruct;
}
//+------------------------------------------------------------------+
//| TradingHistory                                                |
//+------------------------------------------------------------------+
ResultStruct TradingHistory(string details) {
   //Print("details:", details);
   ResultStruct resultStruct;
   uint total = -1;
   ulong ticket = -1; 
   datetime start = 0;
   datetime end = 0;
   string detailsParts[]; 
   StringSplit(details, ';', detailsParts);
   for (int i = 0; i < ArraySize(detailsParts); i++) {
      string key_value[];
      if (StringSplit(detailsParts[i], '>', key_value)) {
         StringTrimRight(key_value[1]);
         StringTrimLeft(key_value[1]);
         if (key_value[0] == "start") {
         start = StringToTime(key_value[1]);
      } else if (key_value[0] == "end") {
         end = StringToTime(key_value[1]);
      } else {
         PlaySound("alert2.wav");
         Print("key_value[0] is NOT PROCESSED: ", key_value[0]);
         resultStruct.code = "FAILURE";
         resultStruct.result = "65537";
         return resultStruct;
      }
      }
   }
   //Print("start:", start, ", end:", end);
   ResetLastError();
   if(!HistorySelect(start, end)) {
      PlaySound("alert2.wav");
      string err_desc = ErrorDescription(GetLastError());
      Print(__FUNCTION__, " HistorySelect = false.", err_desc); 
      resultStruct.code = "FAILURE";
      resultStruct.result = err_desc;
      return resultStruct; 
   } 

   string deals = "\"HistoryDeals\":[";
   total = HistoryDealsTotal(); 
   for(uint i = 0; i < total; i++) {
      if((ticket = HistoryDealGetTicket(i)) > 0) {
         deals += "{";
         deals += "\"ticket\":" + IntegerToString(HistoryDealGetInteger(ticket, DEAL_TICKET));
         deals += ", \"order\":" + IntegerToString(HistoryDealGetInteger(ticket, DEAL_ORDER));
         deals += ", \"time\":" + IntegerToString(HistoryDealGetInteger(ticket, DEAL_TIME));
         deals += ", \"executionTime\":" + IntegerToString(HistoryDealGetInteger(ticket, DEAL_TIME_MSC));
         deals += ", \"type\":" + IntegerToString(HistoryDealGetInteger(ticket, DEAL_TYPE)); // enum
         deals += ", \"entry\":" + IntegerToString(HistoryDealGetInteger(ticket, DEAL_ENTRY)); // enum
         deals += ", \"magic\":" + IntegerToString(HistoryDealGetInteger(ticket, DEAL_MAGIC)); 
         deals += ", \"reason\":" + IntegerToString(HistoryDealGetInteger(ticket, DEAL_REASON)); // enum
         deals += ", \"position\":" + IntegerToString(HistoryDealGetInteger(ticket, DEAL_POSITION_ID)); 
         
         deals += ", \"volume\":" + StringFormat("%G", HistoryDealGetDouble(ticket, DEAL_VOLUME));
         deals += ", \"price\":" + StringFormat("%G", HistoryDealGetDouble(ticket, DEAL_PRICE));
         deals += ", \"commission\":" + StringFormat("%G", HistoryDealGetDouble(ticket, DEAL_COMMISSION));
         deals += ", \"swap\":" + StringFormat("%G", HistoryDealGetDouble(ticket, DEAL_SWAP));
         deals += ", \"profit\":" + StringFormat("%G", HistoryDealGetDouble(ticket, DEAL_PROFIT));
         deals += ", \"fee\":" + StringFormat("%G", HistoryDealGetDouble(ticket, DEAL_FEE));
         deals += ", \"sl\":" + StringFormat("%G", HistoryDealGetDouble(ticket, DEAL_SL));
         deals += ", \"tp\":" + StringFormat("%G", HistoryDealGetDouble(ticket, DEAL_TP));
         
         deals += ", \"symbol\":\"" + HistoryDealGetString(ticket, DEAL_SYMBOL) + "\"";
         deals += ", \"comment\":\"" + HistoryDealGetString(ticket, DEAL_COMMENT) + "\"";
         deals += ", \"external\":\"" + HistoryDealGetString(ticket, DEAL_EXTERNAL_ID) + "\"";
         deals += "}";
         if(i < total - 1) deals += ",";
      }
   }
   deals += "]";
 
   string orders = "\"HistoryOrders\":[";
   total = HistoryOrdersTotal(); 
   for(uint i = 0; i < total; i++) {
      if((ticket = HistoryOrderGetTicket(i)) > 0) {
         orders += "{";
         orders += "\"ticket\":" + IntegerToString(HistoryOrderGetInteger(ticket, ORDER_TICKET));
         orders += ", \"timeSetup\":" + IntegerToString(HistoryOrderGetInteger(ticket, ORDER_TIME_SETUP));// DateTime
         orders += ", \"type\":" + IntegerToString(HistoryOrderGetInteger(ticket, ORDER_TYPE));// enum
         orders += ", \"state\":" + IntegerToString(HistoryOrderGetInteger(ticket, ORDER_STATE));// enum
         orders += ", \"timeExpiration\":" + IntegerToString(HistoryOrderGetInteger(ticket, ORDER_TIME_EXPIRATION));// DateTime
         orders += ", \"timeDone\":" + IntegerToString(HistoryOrderGetInteger(ticket, ORDER_TIME_DONE));// DateTime
         orders += ", \"timeSetupMsc\":" + IntegerToString(HistoryOrderGetInteger(ticket, ORDER_TIME_SETUP_MSC));
         orders += ", \"timeDoneMsc\":" + IntegerToString(HistoryOrderGetInteger(ticket, ORDER_TIME_DONE_MSC));
         orders += ", \"filling\":" + IntegerToString(HistoryOrderGetInteger(ticket, ORDER_TYPE_FILLING));// enum
         orders += ", \"typeTime\":" + IntegerToString(HistoryOrderGetInteger(ticket, ORDER_TYPE_TIME));// enum
         orders += ", \"magic\":" + IntegerToString(HistoryOrderGetInteger(ticket, ORDER_MAGIC));
         orders += ", \"reason\":" + IntegerToString(HistoryOrderGetInteger(ticket, ORDER_REASON));// enum
         orders += ", \"position\":" + IntegerToString(HistoryOrderGetInteger(ticket, ORDER_POSITION_ID));
         orders += ", \"positionById\":" + IntegerToString(HistoryOrderGetInteger(ticket, ORDER_POSITION_BY_ID));
        
         orders += ", \"volumeInitial\":" + StringFormat("%G", HistoryOrderGetDouble(ticket, ORDER_VOLUME_INITIAL));
         orders += ", \"volumeCurrent\":" + StringFormat("%G", HistoryOrderGetDouble(ticket, ORDER_VOLUME_CURRENT));
         orders += ", \"priceOpen\":" + StringFormat("%G", HistoryOrderGetDouble(ticket, ORDER_PRICE_OPEN));
         orders += ", \"sl\":" + StringFormat("%G", HistoryOrderGetDouble(ticket, ORDER_SL));
         orders += ", \"sl\":" + StringFormat("%G", HistoryOrderGetDouble(ticket, ORDER_TP));
         orders += ", \"priceCurrent\":" + StringFormat("%G", HistoryOrderGetDouble(ticket, ORDER_PRICE_CURRENT));
         orders += ", \"priceStopLimit\":" + StringFormat("%G", HistoryOrderGetDouble(ticket, ORDER_PRICE_STOPLIMIT));
        
         deals += ", \"symbol\":\"" + HistoryOrderGetString(ticket, ORDER_SYMBOL) + "\"";
         deals += ", \"comment\":\"" + HistoryOrderGetString(ticket, ORDER_COMMENT) + "\"";
         deals += ", \"external\":\"" + HistoryOrderGetString(ticket, ORDER_EXTERNAL_ID) + "\"";
       
         orders += "}";
         if(i < total - 1) orders += ",";
      }
   }
   orders += "]";

   resultStruct.result = "{" + deals + ", " + orders + "}";
//   resultStruct.result = "{" + orders + "}";
   resultStruct.code = "SUCCESS";
   return resultStruct;
}
//+------------------------------------------------------------------+
//| TradeTransaction function                                        |
//+------------------------------------------------------------------+
void OnTradeTransaction(const MqlTradeTransaction& transaction, const MqlTradeRequest& request, const MqlTradeResult& result) { // 1-5 <-- open and close --> 6-10
   ENUM_TRADE_TRANSACTION_TYPE type = (ENUM_TRADE_TRANSACTION_TYPE)transaction.type;
   if(type == TRADE_TRANSACTION_ORDER_ADD) { // round to open market order: 1, // round to close market order: 6
   } else if (type == TRADE_TRANSACTION_ORDER_UPDATE) {
      Print("TRADE_TRANSACTION_ORDER_UPDATE"); PlaySound("alert2.wav");
   } else if(type == TRADE_TRANSACTION_ORDER_DELETE) { // round to open market order: 3, // round to close market order: 8
      //Print("TRADE_TRANSACTION_ORDER_DELETE"); 
   } else if(type == TRADE_TRANSACTION_DEAL_ADD) { // round to open market order: 2, // round to close market order: 7
      //Print("TRADE_TRANSACTION_DEAL_ADD"); 
   } else if(type == TRADE_TRANSACTION_DEAL_UPDATE) {
      Print("TRADE_TRANSACTION_DEAL_UPDATE"); PlaySound("alert2.wav");
   } else if(type == TRADE_TRANSACTION_DEAL_DELETE) {
      Print("TRADE_TRANSACTION_DEAL_DELETE"); PlaySound("alert2.wav");
   } else if(type == TRADE_TRANSACTION_HISTORY_ADD) { // round to open market order: 4, // round to close market order: 9
      //Print("TRADE_TRANSACTION_HISTORY_ADD"); 
   } else if(type == TRADE_TRANSACTION_HISTORY_UPDATE) {
      Print("TRADE_TRANSACTION_HISTORY_UPDATE"); PlaySound("alert2.wav");
   } else if(type == TRADE_TRANSACTION_HISTORY_DELETE) {
      Print("TRADE_TRANSACTION_HISTORY_DELETE"); PlaySound("alert2.wav");
   } else if(type == TRADE_TRANSACTION_POSITION) { // The TRADE_TRANSACTION_POSITION event fires when the state of a position changes, which can occur due to manual adjustment of stop loss/take profit levels or due to these levels being hit, thereby closing the position.
      //Print("TRADE_TRANSACTION_POSITION"); 
      string symbol = transaction.symbol;
      Sleep(100);  // Give it a moment to process the transaction
      if (PositionSelect(symbol)) {
         transactionDetails = "PositionTicket>" + (string)transaction.position;
         transactionDetails += ", StartOrderStopLoss>" + StringFormat("%G", transaction.price_sl);
         transactionDetails += ", StartOrderTakeProfit>" + StringFormat("%G", transaction.price_tp);
         callResult = ExecutiveMediator::UpdateTransaction(TimeToString(TimeCurrent(), TIME_DATE | TIME_MINUTES | TIME_SECONDS), "TradeCommand", "UpdateTransaction", Ticket, "SUCCESS", transactionDetails);
         //Print(__FUNCTION__, "--->", "callResult:", callResult); // callResult:ok
         if (callResult != OK) { PlaySound("alert2.wav"); }
         transactionDetails = "";
      } else {
         Print("---------- End Position:   ", positionTicket, " -- STTP --");
         if(!GetTradeHistory(1)) { 
            Print(__FUNCTION__, " HistorySelect() returned false");
            PlaySound("alert2.wav");
            return;
         }
         ulong lastOrderTicket = GetLastOrderTicket();
         ulong lastDealTicket = GetLastDealTicket(); 
         transactionDetails = "PositionSymbol>" + HistoryOrderGetString(lastOrderTicket, ORDER_SYMBOL);
         transactionDetails += ", EndOrderTradeType>" + EnumToString((ENUM_ORDER_TYPE)HistoryOrderGetInteger(lastOrderTicket, ORDER_TYPE));
         transactionDetails += ", EndOrderTicket>" + IntegerToString(HistoryOrderGetInteger(lastOrderTicket, ORDER_TICKET));
         transactionDetails += ", EndOrderPrice>" + StringFormat("%G", HistoryOrderGetDouble(lastDealTicket, ORDER_PRICE_OPEN));
         transactionDetails += ", EndOrderAsk>" + StringFormat("%G", 0);
         transactionDetails += ", EndOrderBid>" + StringFormat("%G", 0);
         transactionDetails += ", EndOrderTime>" + TimeToString((datetime)HistoryDealGetInteger(lastDealTicket, DEAL_TIME), TIME_DATE | TIME_MINUTES | TIME_SECONDS);
         //Print(transactionDetails);
         callResult = ExecutiveMediator::UpdateTransaction(TimeToString(TimeCurrent(), TIME_DATE | TIME_MINUTES | TIME_SECONDS), "TradeCommand", "CloseTransaction", Ticket, "SUCCESS", transactionDetails);
         //Print("callResult:", callResult); 
         if (callResult != OK) { PlaySound("alert2.wav"); } else { PlaySound("ok.wav"); }
         positionOpen = false;
         positionTicket = -1;
         transactionDetails = "";
      }  
   } else if(type == TRADE_TRANSACTION_REQUEST) { // round to open market order: 5, // round to close market order: 10
      //Print("TRADE_TRANSACTION_REQUEST"); 
      if (request.action == TRADE_ACTION_DEAL) {
         if (request.type == ORDER_TYPE_BUY || request.type == ORDER_TYPE_SELL) {      
            if (positionOpen == false) {
               FinalizeOpenPosition(transaction, request, result);
            } else {
               FinalizeClosePosition(request, result);
            }
         }
      } 
   } 
}
//+------------------------------------------------------------------+
//| FinalizeClosePosition                                            |
//+------------------------------------------------------------------+
void FinalizeClosePosition(const MqlTradeRequest& request, const MqlTradeResult& result) {
   Print("---------- End Position:   ", positionTicket, " ----------");
   transactionDetails = "PositionSymbol>" + request.symbol;
   transactionDetails += ", EndOrderTradeType>" + EnumToString(request.type);
   transactionDetails += ", EndOrderTicket>" + IntegerToString(result.order);
   transactionDetails += ", EndOrderPrice>" + StringFormat("%G", result.price);
   transactionDetails += ", EndOrderAsk>" + StringFormat("%G", result.ask);
   transactionDetails += ", EndOrderBid>" + StringFormat("%G", result.bid);
   ulong closed_order = result.order;
   datetime timeClose;
   if (HistoryOrderSelect(closed_order)) {
      timeClose = (datetime)HistoryOrderGetInteger(closed_order, ORDER_TIME_DONE);
      transactionDetails += ", EndOrderTime>" + TimeToString(timeClose, TIME_DATE | TIME_MINUTES | TIME_SECONDS);
   }
   //Print(transactionDetails);
   if(result.retcode != 10009) { //TRADE_RETCODE_DONE
      transactionDetails = IntegerToString(result.retcode) + " : " + GetRetcodeID(result.retcode);
      callResult = ExecutiveMediator::UpdateTransaction(TimeToString(TimeCurrent(), TIME_DATE | TIME_MINUTES | TIME_SECONDS), "TradeCommand", "CloseTransaction", Ticket, "FAILURE", transactionDetails);
   } else {
      callResult = ExecutiveMediator::UpdateTransaction(TimeToString(TimeCurrent(), TIME_DATE | TIME_MINUTES | TIME_SECONDS), "TradeCommand", "CloseTransaction", Ticket, "SUCCESS", transactionDetails);
   }
   //Print("callResult:", callResult); // callResult:ok
   if (callResult != OK) { PlaySound("alert2.wav"); }
   positionOpen = false;
   positionTicket = -1;
   transactionDetails = "";
}
//+------------------------------------------------------------------+
//| FinalizeOpenPosition                                             |
//+------------------------------------------------------------------+
void FinalizeOpenPosition(const MqlTradeTransaction& transaction, const MqlTradeRequest& request, const MqlTradeResult& result) {
   positionOpen = true;
   positionTicket = result.order;
   Print("---------- Start Position: ", positionTicket, " ----------");
   transactionDetails = "PositionSymbol>" + request.symbol;
   transactionDetails += ", StartOrderTradeType>" + EnumToString(request.type);
   transactionDetails += ", StartOrderTicket>" + IntegerToString(result.order);
   transactionDetails += ", StartOrderPrice>" + StringFormat("%G", result.price);
   transactionDetails += ", StartOrderStopLoss>" + StringFormat("%G", request.sl);
   transactionDetails += ", StartOrderTakeProfit>" + StringFormat("%G", request.tp);
   transactionDetails += ", StartOrderAsk>" + StringFormat("%G", result.ask);
   transactionDetails += ", StartOrderBid>" + StringFormat("%G", result.bid);
   transactionDetails += ", StartOrderVolume>" + StringFormat("%G", result.volume);
   datetime timeOpen;
   if (PositionSelect(request.symbol)) {
      timeOpen = (datetime)PositionGetInteger(POSITION_TIME);     
      transactionDetails += ", StartOrderTime>" + TimeToString(timeOpen, TIME_DATE | TIME_MINUTES | TIME_SECONDS); 
   } else {
      PlaySound("alert2.wav");
      Print("FinalizeOpenPosition failed with error: timeOpen = (datetime)PositionGetInteger(POSITION_TIME) is false.");
   }
   //Print(transactionDetails);
   if(result.retcode != 10009) { //TRADE_RETCODE_DONE
      transactionDetails = IntegerToString(result.retcode) + " : " + GetRetcodeID(result.retcode);
      callResult = ExecutiveMediator::UpdateTransaction(TimeToString(TimeCurrent(), TIME_DATE | TIME_MINUTES | TIME_SECONDS), "TradeCommand", "OpenTransaction", Ticket, "FAILURE", transactionDetails);
   } else {
      callResult = ExecutiveMediator::UpdateTransaction(TimeToString(TimeCurrent(), TIME_DATE | TIME_MINUTES | TIME_SECONDS), "TradeCommand", "OpenTransaction", Ticket, "SUCCESS", transactionDetails);
   }
   //Print("callResult:", callResult); // callResult:ok
   if (callResult != OK) { PlaySound("alert2.wav"); }
   transactionDetails = "";
}
//+------------------------------------------------------------------+ 
//| Returns transaction textual description                          | 
//+------------------------------------------------------------------+ 

//Print(TransactionDescription(transaction));
//Print(RequestDescription(request));
//Print(ResultDescription(result));

string TransactionDescription(const MqlTradeTransaction &transaction) {
   string desc = "transaction.type: " + EnumToString(transaction.type)+"\r\n"; 
   desc+="transaction.symbol: " + transaction.symbol + "\r\n"; 
   desc+="transaction.deal: " + (string)transaction.deal + "\r\n"; 
   desc+="transaction.deal_type: " + EnumToString(transaction.deal_type) + "\r\n"; 
   desc+="transaction.order: " +(string)transaction.order + "\r\n"; 
   desc+="transaction.order_type: " + EnumToString(transaction.order_type) + "\r\n"; 
   desc+="transaction.order_state: " + EnumToString(transaction.order_state) + "\r\n"; 
   desc+="transaction.time_type: " + EnumToString(transaction.time_type) + "\r\n"; 
   desc+="transaction.time_expiration: " + TimeToString(transaction.time_expiration) + "\r\n"; 
   desc+="transaction.price: " + StringFormat("%G", transaction.price) + "\r\n"; 
   desc+="transaction.price_trigger: " + StringFormat("%G",transaction.price_trigger) + "\r\n"; 
   desc+="transaction.price_sl: " + StringFormat("%G", transaction.price_sl) + "\r\n"; 
   desc+="transaction.price_tp: " + StringFormat("%G", transaction.price_tp) + "\r\n"; 
   desc+="transaction.volume: " + StringFormat("%G", transaction.volume) + "\r\n"; 
   desc+="transaction.position: " + (string)transaction.position + "\r\n"; 
   desc+="transaction.position_by: " + (string)transaction.position_by + "\r\n";
   return desc; 
} 
//+------------------------------------------------------------------+ 
//| Returns the trade request textual description                    | 
//+------------------------------------------------------------------+ 
string RequestDescription(const MqlTradeRequest &request) { 
   string desc = "request.action: " + EnumToString(request.action) + "\r\n"; 
   desc+="request.symbol: " + request.symbol + "\r\n"; 
   desc+="request.magic: " + StringFormat("%d", request.magic) + "\r\n"; 
   desc+="request.order: " + (string)request.order + "\r\n"; 
   desc+="request.type: " + EnumToString(request.type) + "\r\n"; 
   desc+="request.type_filling: " + EnumToString(request.type_filling) + "\r\n"; 
   desc+="request.type_time: " + EnumToString(request.type_time) + "\r\n"; 
   desc+="request.expiration: " + TimeToString(request.expiration) + "\r\n"; 
   desc+="request.price: " + StringFormat("%G", request.price) + "\r\n"; 
   desc+="request.deviation: " + StringFormat("%G", request.deviation) + "\r\n"; 
   desc+="request.sl: " + StringFormat("%G", request.sl) + "\r\n"; 
   desc+="Trequest.tp: " + StringFormat("%G", request.tp) + "\r\n"; 
   desc+="request.stoplimit: " + StringFormat("%G", request.stoplimit) + "\r\n"; 
   desc+="request.volume: " + StringFormat("%G", request.volume) + "\r\n"; 
   desc+="request.comment: " + request.comment + "\r\n"; 
   return desc; 
} 
//+------------------------------------------------------------------+ 
//| Returns the textual description of the request handling result   | 
//+------------------------------------------------------------------+ 
string ResultDescription(const MqlTradeResult &result) {
   string desc = "result.retcode " + (string)result.retcode + "\r\n"; 
   desc+="result.request_id: " + StringFormat("%d", result.request_id) + "\r\n"; 
   desc+="result.order: " + (string)result.order + "\r\n"; 
   desc+="result.deal: " + (string)result.deal + "\r\n"; 
   desc+="result.volume: " + StringFormat("%G", result.volume) + "\r\n"; 
   desc+="result.price: " + StringFormat("%G", result.price) + "\r\n"; 
   desc+="result.ask: " + StringFormat("%G", result.ask) + "\r\n"; 
   desc+="result.bid: " + StringFormat("%G", result.bid) + "\r\n"; 
   desc+="result.comment: " + result.comment + "\r\n"; 
   return desc; 
}
//+------------------------------------------------------------------+ 
//| Returns the last order ticket in history or -1                   | 
//+------------------------------------------------------------------+ 
ulong GetLastOrderTicket() { 
   ulong first_order, last_order, orders = HistoryOrdersTotal(); 
   if(orders > 0) {
      first_order = HistoryOrderGetTicket(0);
      if(orders > 1) { 
         last_order = HistoryOrderGetTicket((int)orders - 1); 
         return last_order; 
      } 
      return first_order; 
   } 
   return -1; 
}
//+------------------------------------------------------------------+ 
//| Returns the last deal ticket in history or -1                    | 
//+------------------------------------------------------------------+ 
ulong GetLastDealTicket() { 
   ulong first_deal, last_deal, deals = HistoryDealsTotal(); 
   if(deals > 0) {
      first_deal = HistoryDealGetTicket(0);
      if(deals > 1) { 
         last_deal = HistoryDealGetTicket((int)deals - 1); 
         return last_deal; 
      } 
      return first_deal; 
   } 
   return -1; 
}
//+--------------------------------------------------------------------------+ 
//| Requests history for the last days and returns false in case of failure  | 
//+--------------------------------------------------------------------------+ 
bool GetTradeHistory(int days) { 
   datetime to = TimeCurrent(); 
   datetime from = to - days * PeriodSeconds(PERIOD_D1);
   ResetLastError();
   if(!HistorySelect(from, to)) { 
      Print(__FUNCTION__, " HistorySelect=false. Error code=", GetLastError()); 
      return false; 
   } 
   return true; 
}
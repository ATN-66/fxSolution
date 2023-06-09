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
string const OK = "ok";
int const ENVIRONMENT = 1; //Development = 1, Production = 4

string callResult;
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
int OnInit() {

   callResult = ExecutiveMediator::Init(TimeToString(TimeCurrent(), TIME_DATE | TIME_MINUTES | TIME_SECONDS));
   bool status = CheckCallStatus(callResult);
   
   // If call status check failed, terminate initialization
   if (!status) {
      PlaySound("disconnect.wav"); 
      return INIT_FAILED;
   }
   
   EventSetTimer(1);
   connected = true;
   return INIT_SUCCEEDED;
}
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason) {

   if(!connected) return;
   
   ExecutiveMediator::DeInit(TimeToString(TimeCurrent(), TIME_DATE | TIME_MINUTES | TIME_SECONDS));          
   EventKillTimer();//--- destroy timer
}
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick(){
}
//+------------------------------------------------------------------+
//| Timer function                                                   |
//+------------------------------------------------------------------+
void OnTimer(){
   
   callResult = ExecutiveMediator::Pulse(TimeToString(TimeCurrent(), TIME_DATE | TIME_MINUTES | TIME_SECONDS), "0", "0", "0", "0");
   if(callResult != OK)
   {
      //Print(result); // "Type: AccountInfo, Code: IntegerAccountProperties, Ticket: 1"
      string Type;
      string Code;
      string Ticket;
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
            }
         }
      }
      
      Print("Type:", Type, ", Code:", Code, ", Ticket:", Ticket);
      
      if(Type == "AccountInfo"){
         if(Code == "IntegerAccountProperties"){
            callResult = ExecutiveMediator::IntegerAccountProperties(TimeToString(TimeCurrent(), TIME_DATE | TIME_MINUTES | TIME_SECONDS), Type, Code, Ticket, account.Login(), account.TradeMode(), account.Leverage(), account.StopoutMode(), account.MarginMode(), account.TradeAllowed(), account.TradeExpert(), account.LimitOrders());
         } else if(Code == "DoubleAccountProperties"){
            callResult = ExecutiveMediator::DoubleAccountProperties(TimeToString(TimeCurrent(), TIME_DATE | TIME_MINUTES | TIME_SECONDS), Type, Code, Ticket, account.Balance(), account.Credit(), account.Profit(), account.Equity(), account.Margin(), account.FreeMargin(), account.MarginLevel(), account.MarginCall(), account.MarginStopOut());
         }
         else if(Code == "StringAccountProperties"){
            callResult = ExecutiveMediator::StringAccountProperties(TimeToString(TimeCurrent(), TIME_DATE | TIME_MINUTES | TIME_SECONDS), Type, Code, Ticket, account.Name(), account.Server(), account.Currency(), account.Company());
         }
         else if(Code == "MaxVolumes"){
            callResult = ExecutiveMediator::MaxVolumes(TimeToString(TimeCurrent(), TIME_DATE | TIME_MINUTES | TIME_SECONDS), Type, Code, Ticket, MaxVolumes());
         }
      }
   }
}
//+------------------------------------------------------------------+
//| Trade function                                                   |
//+------------------------------------------------------------------+
void OnTrade() {
}
//+------------------------------------------------------------------+
//| TradeTransaction function                                        |
//+------------------------------------------------------------------+
void OnTradeTransaction(const MqlTradeTransaction& trans, const MqlTradeRequest& transRequest, const MqlTradeResult& transResult) {   
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CheckCallStatus(string result) {
    string parts[];
    int length = StringSplit(result, ':', parts);

    if(length >= 3) { // we expect at least 3 parts
        string entity = parts[0];
        string guid = parts[1];
        string status = parts[2];

        if(status == OK) {
            // Call succeeded
            Print("Call succeeded for entity: ", entity, " with GUID: ", guid);
            return true;
        } else {
            // Call failed
            Print("Call failed for entity: ", entity, " with GUID: ", guid, ". Status: ", status);
            return false;
        }
    } else {
        Print("Invalid callResult string");
        Print(result);
        return false;
    }
}
//+------------------------------------------------------------------+
//| OpenOrder                                                        |
//+------------------------------------------------------------------+
void OpenOrder(string symbol, ENUM_ORDER_TYPE orderType, double volume, double riskPercent, double profitPercent) {
   MqlTradeRequest request;
   MqlTradeResult result;

   double price;
   double pointValue = SymbolInfoDouble(symbol, SYMBOL_TRADE_TICK_VALUE);  // the monetary value of a single point

Print(symbol, " pointValue: ", pointValue);
return;

   // Retrieve the appropriate price
   if (orderType == ORDER_TYPE_BUY) {
      price = SymbolInfoDouble(symbol, SYMBOL_ASK);
   } else if (orderType == ORDER_TYPE_SELL) {
      price = SymbolInfoDouble(symbol, SYMBOL_BID);
   }

   // Calculate stop loss and take profit in pips based on account balance
   //double stopLossPips = stopLossRisk * AccountInfoDouble(ACCOUNT_BALANCE) / (pointValue * volume);
   //double takeProfitPips = takeProfitReward * AccountInfoDouble(ACCOUNT_BALANCE) / (pointValue * volume);

   // Calculate stop loss and take profit in pips based on account balance
   double accountBalance = AccountInfoDouble(ACCOUNT_BALANCE);
   double accountCurrencyToQuoteCurrencyRate = 1.0;
   
   if (Symbol() != "CADJPY" && StringSubstr(Symbol(), 3, 3) == "JPY") {
       MqlTick tick;
       if (!SymbolInfoTick("CADJPY", tick)) {
           Print("Error getting tick value for CADJPY");
           return;
       }
       accountCurrencyToQuoteCurrencyRate = tick.ask;  // use the ask price as the exchange rate
   }
   
   //double stopLossRiskAmount = stopLossRisk * accountBalance * accountCurrencyToQuoteCurrencyRate;
   //double takeProfitRewardAmount = takeProfitReward * accountBalance * accountCurrencyToQuoteCurrencyRate;
   
   //double stopLossPips = stopLossRiskAmount / (pointValue * volume);
   //double takeProfitPips = takeProfitRewardAmount / (pointValue * volume);
   
   // rest of the code...








   // Convert pips to price levels
   //double stopLoss, takeProfit;
   //if (orderType == ORDER_TYPE_BUY) {
      //stopLoss = price - stopLossPips * SymbolInfoDouble(symbol, SYMBOL_TRADE_TICK_SIZE);
      //takeProfit = price + takeProfitPips * SymbolInfoDouble(symbol, SYMBOL_TRADE_TICK_SIZE);
   //} else if (orderType == ORDER_TYPE_SELL) {
      //stopLoss = price + stopLossPips * SymbolInfoDouble(symbol, SYMBOL_TRADE_TICK_SIZE);
      //takeProfit = price - takeProfitPips * SymbolInfoDouble(symbol, SYMBOL_TRADE_TICK_SIZE);
  // }

   //request.symbol = symbol;
   //request.volume = volume;
   //request.price = price;
  // request.sl = stopLoss;
  // request.tp = takeProfit;
  // request.type = orderType;
   //request.action = TRADE_ACTION_DEAL;
  // request.deviation = 10;
  // request.magic = 0;
  // request.comment = "Order opened by my EA";

   //OrderSend(request, result);
   //Print("Order executed with result: ", GetTradeResultDescription(result.retcode));
}
//+------------------------------------------------------------------+
//| GetTradeResultDescription                                        |
//+------------------------------------------------------------------+
string GetTradeResultDescription(int retcode) {
   string resultDescription;
   switch(retcode) {
      case TRADE_RETCODE_DONE: resultDescription = "Request completed successfully."; break;
      case TRADE_RETCODE_INVALID_VOLUME: resultDescription = "Invalid volume."; break;
      case TRADE_RETCODE_INVALID_PRICE: resultDescription = "Invalid price."; break;
      // add cases for other retcodes as per the MQL5 documentation
      // ...
      default: resultDescription = "Unknown error. Code: " + IntegerToString(retcode); break;
   }
   return resultDescription;
}
//+------------------------------------------------------------------+
//| MaxVolumes                                                       |
//+------------------------------------------------------------------+
string MaxVolumes() {
    ENUM_ORDER_TYPE trade_operation = ORDER_TYPE_BUY;  // or ORDER_TYPE_SELL
    string symbols[] = {"EURUSD", "GBPUSD", "USDJPY", "EURGBP", "EURJPY", "GBPJPY"};
    string maxVolumes = "";

    for (int i = 0; i < ArraySize(symbols); i++)
    {
        double price = SymbolInfoDouble(symbols[i], SYMBOL_BID);  // or SYMBOL_ASK
        double maxVolume = account.MaxLotCheck(symbols[i], trade_operation, price);

        // Add to the maxVolumes string
        maxVolumes += symbols[i] + ":" + DoubleToString(maxVolume, 2);  // adjust the second parameter of DoubleToString for desired decimal places

        // Add comma for all but last
        if (i < ArraySize(symbols) - 1)
        {
            maxVolumes += ", ";
        }
    }
    return maxVolumes;
}
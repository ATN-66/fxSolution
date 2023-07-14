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
input int MagicNumber=1234567;

string callResult;
bool connected = false;
int id = 0;

//--- flags for installing and deleting the pending order 
bool pending_done=false; 
bool pending_deleted=false; 
//--- pending order ticket will be stored here 
ulong order_ticket; 
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit() {
   string initResult = ExecutiveMediator::Init(TimeToString(TimeCurrent(), TIME_DATE | TIME_MINUTES | TIME_SECONDS));
   bool status = CheckInitStatus(initResult);
   // If call status check failed, terminate initialization
   if (!status) {
      PlaySound("disconnect.wav"); 
      return INIT_FAILED;
   }
   //--- set MagicNumber to mark all our orders 
   trade.SetExpertMagicNumber(MagicNumber); 
   //--- trade requests will be sent in asynchronous mode using OrderSendAsync() function 
   trade.SetAsyncMode(true); 
   //--- initialize the variable by zero 
   order_ticket=0; 
   //--- 
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
   callResult = ExecutiveMediator::Pulse(TimeToString(TimeCurrent(), TIME_DATE | TIME_MINUTES | TIME_SECONDS), "0", "0", "0", "0");
   if(callResult != OK)
   {
      //Print(callResult); // 
      string Type;
      string Code;
      string Ticket;
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
      
      // Print("Type:", Type, ", Code:", Code, ", Ticket:", Ticket, ", Details:", Details);
      
      if(Type == "AccountInfo"){
         if(Code == "MaxVolumes"){
            callResult = ExecutiveMediator::MaxVolumes(TimeToString(TimeCurrent(), TIME_DATE | TIME_MINUTES | TIME_SECONDS), Type, Code, Ticket, MaxVolumes());
         } else if(Code == "AccountProperties"){
            callResult = ExecutiveMediator::AccountProperties(TimeToString(TimeCurrent(), TIME_DATE | TIME_MINUTES | TIME_SECONDS), Type, Code, Ticket, AccountProperties());
         }
         else {
            Print("Code is NOT PROCESSED: ", Code);
            return;
         } 
      }
      else if(Type == "TradeCommand"){
         if(Code == "OpenPosition"){
            OpenOrder(Details);
         } else {
            Print("Code is NOT PROCESSED: ", Code);
            return;
         } 
      }
      else {
         Print("Type is NOT PROCESSED: ", Type);
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
//| AccountProperties                                                |
//+------------------------------------------------------------------+
string AccountProperties() {
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
   return accountProperties;
}
//+------------------------------------------------------------------+
//| OpenOrder                                                        |
//+------------------------------------------------------------------+
void OpenOrder(string details) {// "symbol>EURUSD;ordertype>OrderTypeSell;volume>1.50;stoplossinpips>10"
   
   string symbol;
   ENUM_ORDER_TYPE ordertype = -1;
   double volume = -1;
   double stoplossinpips = -1;
   string detailsParts[];
   double price = -1;
   double stopLoss = -1;
   MqlTradeRequest request;
   MqlTradeResult result;
   
   StringSplit(details, ';', detailsParts);
   for (int i = 0; i < ArraySize(detailsParts); i++) {
      string key_value[];
      if (StringSplit(detailsParts[i], '>', key_value)) {
         StringTrimRight(key_value[1]);
         StringTrimLeft(key_value[1]);
         
         if (key_value[0] == "symbol") {
            symbol = key_value[1];
            Print("symbol:", symbol); // symbol:EURUSD
         } else if (key_value[0] == "ordertype") {
            if (key_value[1] == "OrderTypeSell") {
               ordertype = ORDER_TYPE_SELL;
            } else if (key_value[1] == "OrderTypeBuy") {
               ordertype = ORDER_TYPE_BUY;
            } else {
               Print("ordertype is NOT PROCESSED: ", ordertype);
               return;
            }
            Print("ordertype:", ordertype); // ordertype:1
         } else if (key_value[0] == "volume") {
            volume = StringToDouble(key_value[1]);
            Print("volume:", volume); // volume:1.5
         } else if (key_value[0] == "stoplossinpips") {
            stoplossinpips = StringToDouble(key_value[1]);
            Print("stoplossinpips:", stoplossinpips); // stoplossinpips:10.0
         } else {
            Print("key_value[0] is NOT PROCESSED: ", key_value[0]);
            return;
         }
      }
   }

   //Retrieve the appropriate price
   if (ordertype == ORDER_TYPE_BUY) {
      price = SymbolInfoDouble(symbol, SYMBOL_ASK);
      stopLoss = price - stoplossinpips * SymbolInfoDouble(symbol, SYMBOL_TRADE_TICK_SIZE);
   } else if (ordertype == ORDER_TYPE_SELL) {
      price = SymbolInfoDouble(symbol, SYMBOL_BID);
      stopLoss = price + stoplossinpips * SymbolInfoDouble(symbol, SYMBOL_TRADE_TICK_SIZE);
   } else {
      Print("NOT PROCESSED: ", ordertype);
      return;
   }
   Print("price:", price); // price:1.12228
   Print("stopLoss:", stopLoss); // price:1.12228

   request.symbol = symbol;
   request.volume = volume;
   request.price = price;
   request.sl = stopLoss;
   request.type = ordertype;
   request.action = TRADE_ACTION_DEAL;
   request.deviation = 10;
   request.magic = 0;
   request.comment = "Order opened by EXECUTIVE EA";
   
   if (!OrderSend(request, result)) {
      int err = GetLastError();
      Print("OrderSend failed with error: ", err, ": ", ErrorDescription(err));
      ResetLastError();
   } else {
      Print("OrderSend succeeded with result: ", result.retcode, ": ", SuccessDescription(result.retcode));
   }
}
//+------------------------------------------------------------------+
//| ErrorDescription                                                 |
//+------------------------------------------------------------------+
string ErrorDescription(int err_code) {
    switch(err_code) {
        case TRADE_RETCODE_MARKET_CLOSED: return "Market is closed";
        case TRADE_RETCODE_INVALID_VOLUME: return "Invalid volume in the request";
        case TRADE_RETCODE_INVALID_PRICE: return "Invalid price in the request";
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
//+------------------------------------------------------------------+
//| TradeTransaction function                                        |
//+------------------------------------------------------------------+
void OnTradeTransaction(const MqlTradeTransaction& trans, const MqlTradeRequest& transRequest, const MqlTradeResult& transResult) {
   //--- get transaction type as enumeration value  
   ENUM_TRADE_TRANSACTION_TYPE type=(ENUM_TRADE_TRANSACTION_TYPE)trans.type; 
   //--- if the transaction is the request handling result, only its name is displayed 
   if(type==TRADE_TRANSACTION_REQUEST) { 
      Print(EnumToString(type)); 
      //--- display the handled request string name 
      Print("------------RequestDescription\r\n",RequestDescription(transRequest)); 
      //--- display request result description 
      Print("------------ResultDescription\r\n",TradeResultDescription(transResult)); 
      //--- store the order ticket for its deletion at the next handling in OnTick() 
      if(transResult.order!=0) { 
         //--- delete this order by its ticket at the next OnTick() call 
         order_ticket=transResult.order; 
         Print(" Pending order ticket ",order_ticket,"\r\n"); 
      } 
   } else // display the full description for transactions of another type 
      //--- display description of the received transaction in the Journal 
      Print("------------TransactionDescription\r\n",TransactionDescription(trans));     
}
//+------------------------------------------------------------------+ 
//| Returns transaction textual description                          | 
//+------------------------------------------------------------------+ 
string TransactionDescription(const MqlTradeTransaction &trans) {
   string desc=EnumToString(trans.type)+"\r\n"; 
   desc+="Symbol: "+trans.symbol+"\r\n"; 
   desc+="Deal ticket: "+(string)trans.deal+"\r\n"; 
   desc+="Deal type: "+EnumToString(trans.deal_type)+"\r\n"; 
   desc+="Order ticket: "+(string)trans.order+"\r\n"; 
   desc+="Order type: "+EnumToString(trans.order_type)+"\r\n"; 
   desc+="Order state: "+EnumToString(trans.order_state)+"\r\n"; 
   desc+="Order time type: "+EnumToString(trans.time_type)+"\r\n"; 
   desc+="Order expiration: "+TimeToString(trans.time_expiration)+"\r\n"; 
   desc+="Price: "+StringFormat("%G",trans.price)+"\r\n"; 
   desc+="Price trigger: "+StringFormat("%G",trans.price_trigger)+"\r\n"; 
   desc+="Stop Loss: "+StringFormat("%G",trans.price_sl)+"\r\n"; 
   desc+="Take Profit: "+StringFormat("%G",trans.price_tp)+"\r\n"; 
   desc+="Volume: "+StringFormat("%G",trans.volume)+"\r\n"; 
   desc+="Position: "+(string)trans.position+"\r\n"; 
   desc+="Position by: "+(string)trans.position_by+"\r\n"; 
   //--- return the obtained string 
   return desc; 
} 
//+------------------------------------------------------------------+ 
//| Returns the trade request textual description                    | 
//+------------------------------------------------------------------+ 
string RequestDescription(const MqlTradeRequest &request) {
   string desc=EnumToString(request.action)+"\r\n"; 
   desc+="Symbol: "+request.symbol+"\r\n"; 
   desc+="Magic Number: "+StringFormat("%d",request.magic)+"\r\n"; 
   desc+="Order ticket: "+(string)request.order+"\r\n"; 
   desc+="Order type: "+EnumToString(request.type)+"\r\n"; 
   desc+="Order filling: "+EnumToString(request.type_filling)+"\r\n"; 
   desc+="Order time type: "+EnumToString(request.type_time)+"\r\n"; 
   desc+="Order expiration: "+TimeToString(request.expiration)+"\r\n"; 
   desc+="Price: "+StringFormat("%G",request.price)+"\r\n"; 
   desc+="Deviation points: "+StringFormat("%G",request.deviation)+"\r\n"; 
   desc+="Stop Loss: "+StringFormat("%G",request.sl)+"\r\n"; 
   desc+="Take Profit: "+StringFormat("%G",request.tp)+"\r\n"; 
   desc+="Stop Limit: "+StringFormat("%G",request.stoplimit)+"\r\n"; 
   desc+="Volume: "+StringFormat("%G",request.volume)+"\r\n"; 
   desc+="Comment: "+request.comment+"\r\n"; 
//--- return the obtained string 
   return desc; 
} 
//+------------------------------------------------------------------+ 
//| Returns the textual description of the request handling result   | 
//+------------------------------------------------------------------+ 
string TradeResultDescription(const MqlTradeResult &result) {
   string desc="Retcode "+(string)result.retcode+"\r\n"; 
   desc+="Request ID: "+StringFormat("%d",result.request_id)+"\r\n"; 
   desc+="Order ticket: "+(string)result.order+"\r\n"; 
   desc+="Deal ticket: "+(string)result.deal+"\r\n"; 
   desc+="Volume: "+StringFormat("%G",result.volume)+"\r\n"; 
   desc+="Price: "+StringFormat("%G",result.price)+"\r\n"; 
   desc+="Ask: "+StringFormat("%G",result.ask)+"\r\n"; 
   desc+="Bid: "+StringFormat("%G",result.bid)+"\r\n"; 
   desc+="Comment: "+result.comment+"\r\n"; 
   //--- return the obtained string 
   return desc; 
}
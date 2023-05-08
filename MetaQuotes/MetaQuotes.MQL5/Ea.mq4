//+------------------------------------------------------------------+
//| Ea                                                        Ea.mq4 |
//|                                 Copyright © 2017, Andrew Nikulin |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2017, Andrew Nikulin"
#import "Ea.dll"
void EaDeinit();
void EaInit(int mode);//In Terminal: NaN = 0,Demo = 1,Contest = 2,Real = 3,Simulation = 4; //In EA: AccountTradeModeDemo = 0, AccountTradeModeContest = 1, AccountTradeModeReal = 2

void EaAccountBalance(double equity, double profit);
void EaAccountLeverage(int accountLeverage);
void EaAccountFreeMargin(double accountFreeMargin);
void EaAccountMarginRequired(int symbol, double marginRequired);
void EaAccountLotSize(int symbol, double lotSize);
void EaAccountMinLot(int symbol, double minLot);
void EaAccountLotStep(int symbol, double lotStep);
void EaAccountMaxLot(int symbol, double maxLot);
void EaAccountRefresh();

string TerminalRequest();
void OpenDone(int ticket, int openTime, int openPrice, int closePrice, double commision);
void CloseDone(int ticket, int closeTime, int closePrice);
void TradeInfo(int closePrice, double profit);
void Error(int sender, int error);
void Market(bool isTradeAllowed, int elapsedSeconds);

int stopTradeHour = 0;
int startTradeHour = 25;
bool market = false;
string terminalRequest;
double lots;
int ticket;
int lastError;

void deinit(){ EaDeinit(); EventKillTimer(); }
void init(){
   if(AccountCurrency() != "USD"){ PlaySound("disconnect.wav"); return; }
   if(Symbol() != "EURUSD"){ PlaySound("disconnect.wav"); return; }
        
   EaInit(AccountInfoInteger(ACCOUNT_TRADE_MODE) + 1);

   market = false;   
   Sleep(3000);
   EventSetTimer(1);
}
   
void OnTimer(){
   if(market && (TimeHour(TimeCurrent()) >= stopTradeHour || TimeHour(TimeCurrent()) < startTradeHour)) { market = false; Market(market, TimeCurrent()); }
   else if(!market && TimeHour(TimeCurrent()) < stopTradeHour && TimeHour(TimeCurrent()) >= startTradeHour) { market = true; Market(market, TimeCurrent()); }
   
   if(ticket != 0){
      if(OrderSelect(ticket, SELECT_BY_TICKET, MODE_TRADES)){
         switch(OrderType())
         {
            case OP_BUY: TradeInfo(MarketInfo(OrderSymbol(), MODE_BID)* 100000, OrderProfit()); break;
            case OP_SELL: TradeInfo(MarketInfo(OrderSymbol(), MODE_ASK)* 100000, OrderProfit()); break;
         }
      }else Error(60, GetLastError());
   }

   terminalRequest = TerminalRequest();
   if(terminalRequest == "") return;
   string sep = ",";
   ushort u_sep = StringGetCharacter(sep, 0);
   string result[];
   int k = StringSplit(terminalRequest, u_sep, result);
   if (result[0] == "Refresh"){ AccountRefresh(); }
   else if (result[0] == "TradeHours"){ 
      stopTradeHour = result[1];
      startTradeHour = result[2];
      if((TimeHour(TimeCurrent()) >= stopTradeHour || TimeHour(TimeCurrent()) < startTradeHour)) { market = false; Market(market, TimeCurrent()); }  
      else if(TimeHour(TimeCurrent()) < stopTradeHour && TimeHour(TimeCurrent()) >= startTradeHour) { market = true; Market(market, TimeCurrent()); }
      Print(stopTradeHour, ", ", startTradeHour, ", ", market);
   }
   else if (result[1] == "Buy"){
      lots = result[2];
      if(AccountFreeMarginCheck(result[0], OP_BUY, lots) >= 0){
         while(true){
            ResetLastError();
            ticket = OrderSend(result[0], OP_BUY, lots, MarketInfo(result[0], MODE_ASK), 2, 0, 0);
            if(ticket > 0) break;
            lastError = GetLastError();
            if(lastError == ERR_REQUOTE) continue; 
            else{ Error(21, lastError); return; }
         }//while
         if (OrderSelect(ticket, SELECT_BY_TICKET, MODE_TRADES)){ OpenDone(ticket, OrderOpenTime(), OrderOpenPrice() * 100000, MarketInfo(OrderSymbol(), MODE_BID)* 100000, OrderProfit()); } else Error(22, GetLastError());
      }
      else Error(20, GetLastError());
   }
   else if (result[1] == "Sell"){
      lots = result[2];
      if(AccountFreeMarginCheck(result[0], OP_SELL, lots) >= 0){
         while(true){
            ResetLastError();
            ticket = OrderSend(result[0], OP_SELL, lots, MarketInfo(result[0], MODE_BID), 2, 0, 0);
            if(ticket > 0) break;
            lastError = GetLastError();
            if(lastError == ERR_REQUOTE) continue;
            else{ Error(31, lastError); return; }
         }//while
         if (OrderSelect(ticket, SELECT_BY_TICKET, MODE_TRADES)){ OpenDone(ticket, OrderOpenTime(), OrderOpenPrice() * 100000, MarketInfo(OrderSymbol(), MODE_ASK)* 100000, OrderProfit()); } else Error(32, GetLastError());
      }
      else Error(30, GetLastError());
   }
   else if (result[0] == "Out"){
      ticket = StrToInteger(result[1]);
      string symbol;
      int operation;
      int mode;
      if (OrderSelect(ticket, SELECT_BY_TICKET, MODE_TRADES)){
         symbol = OrderSymbol();
         lots = OrderLots();
         operation = OrderType();
         if (operation == OP_BUY) mode = MODE_BID;
         else if (operation == OP_SELL) mode = MODE_ASK;
         else { Error(41, GetLastError()); return; }
         while(true){
            ResetLastError();
            RefreshRates(); 
            double price = MarketInfo(symbol, mode);
            if(OrderClose(ticket, lots, price, 2)) break;
            lastError = GetLastError();
            if(lastError == ERR_REQUOTE) continue; else{ Error(42, lastError); return; }
         }//while
         if(OrderSelect(ticket, SELECT_BY_TICKET, MODE_HISTORY)){
            CloseDone(ticket, OrderCloseTime(), OrderClosePrice() * 100000); 
            ticket = 0;
         }
         else{ Error(43, lastError); return; }
      }
      else { Error(40, lastError); return; }
   }
   else { Print("Unprocessed request: ", terminalRequest); }
}

void AccountRefresh(){
   if(OrderSelect(0, SELECT_BY_POS, MODE_TRADES)) { Error(666, lastError); return; }
   if(AccountBalance() != AccountEquity()) { Error(667, lastError); return; }

   int i, hstTotal=OrdersHistoryTotal();
   double profit;
   for(i=0; i < hstTotal; i++){
      if(OrderSelect(i, SELECT_BY_POS, MODE_HISTORY) == false){ Error(50, GetLastError()); break; }
      if(OrderType() != OP_BUY && OrderType() != OP_SELL) continue;
      if(TimeYear(TimeCurrent()) == TimeYear(OrderCloseTime()) && TimeMonth(TimeCurrent()) == TimeMonth(OrderCloseTime()) && TimeDay(TimeCurrent()) == TimeDay(OrderCloseTime())) { profit = profit + OrderProfit(); }
   }
   
   EaAccountBalance(AccountBalance(), profit);
   EaAccountLeverage(AccountLeverage());
   EaAccountFreeMargin(AccountFreeMargin());
   EaAccountMarginRequired(0, MarketInfo("GBPUSD", MODE_MARGINREQUIRED));
   EaAccountMarginRequired(1, MarketInfo("EURUSD", MODE_MARGINREQUIRED));
   EaAccountLotSize(0, MarketInfo("GBPUSD", MODE_LOTSIZE));
   EaAccountLotSize(1, MarketInfo("EURUSD", MODE_LOTSIZE));
   EaAccountMinLot(0, MarketInfo("GBPUSD", MODE_MINLOT));
   EaAccountMinLot(1, MarketInfo("EURUSD", MODE_MINLOT));
   EaAccountLotStep(0, MarketInfo("GBPUSD", MODE_LOTSTEP));
   EaAccountLotStep(1, MarketInfo("EURUSD", MODE_LOTSTEP));
   EaAccountMaxLot(0, MarketInfo("GBPUSD", MODE_MAXLOT));
   EaAccountMaxLot(1, MarketInfo("EURUSD", MODE_MAXLOT));
   EaAccountRefresh();
}

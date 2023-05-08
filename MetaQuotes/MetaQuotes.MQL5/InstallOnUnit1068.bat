@ECHO OFF
mode con: cols=100 lines=100
@SET userame=andre
@SET instance_id=9B101088254A9C260A9790D5079A7B11
@SET DownloadsFolder=C:\Users\Public\Downloads\
@SET IndicatorsFolder=C:\Users\%userame%\AppData\Roaming\MetaQuotes\Terminal\%instance_id%\MQL5\Indicators\Examples\
@SET LibrariesFolder=C:\Users\%userame%\AppData\Roaming\MetaQuotes\Terminal\%instance_id%\MQL5\Libraries\
@ECHO ON
@ECHO OFF
rem ------------------------------------------------------------------------------------------------------------------------------
rem @SET ExpertsFolder=C:\Users\andre\AppData\Roaming\MetaQuotes\Terminal\CF48736A04CB4E277F336167170AB43B\MQL4\Experts\
rem move %DownloadsFolder%Ea.mq4 %ExpertsFolder% 
rem move %DownloadsFolder%Ea.dll %ExpertsFolder% 
rem ------------------------------------------------------------------------------------------------------------------------------
@ECHO ON
move %DownloadsFolder%Indicator.mq5 %IndicatorsFolder% 
move %DownloadsFolder%MetaQuotes.Prototype.Indicator.PipeMethodCalls.dll %LibrariesFolder%
rem ------------------------------------------------------------------------------------------------------------------------------
move %DownloadsFolder%MT5\Libraries\*.* C:\MT5\Libraries\
pause
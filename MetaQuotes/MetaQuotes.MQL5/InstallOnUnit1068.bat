@ECHO ON
mode con: cols=100 lines=100
@SET userame=andre
@SET instance_id=9B101088254A9C260A9790D5079A7B11
rem ------------------------------------------------------------------------------------------------------------------------------
@SET DownloadedDLLFolder=C:\SharedFolder\MT5\DLL\
@SET DownloadedLibrariesFolder=C:\SharedFolder\MT5\Libraries\
@SET DownloadedScriptsFolder=C:\SharedFolder\MT5\Scripts\
rem ------------------------------------------------------------------------------------------------------------------------------
@SET DLLFolder=C:\Users\%userame%\AppData\Roaming\MetaQuotes\Terminal\%instance_id%\MQL5\Libraries\
@SET LibrariesFolder=C:\MQL5\Libraries\
@SET IndicatorsFolder=C:\Users\%userame%\AppData\Roaming\MetaQuotes\Terminal\%instance_id%\MQL5\Indicators\Examples\
rem ------------------------------------------------------------------------------------------------------------------------------
move %DownloadedDLLFolder%*.* %DLLFolder% 
move %DownloadedLibrariesFolder%*.* %LibrariesFolder% 
move %DownloadedScriptsFolder%*.* %IndicatorsFolder% 
pause
@ECHO OFF
mode con: cols=100 lines=100
@SET userame=andre
@SET instance_id=9B101088254A9C260A9790D5079A7B11
rem ------------------------------------------------------------------------------------------------------------------------------
@SET DownloadedDLLFolder=C:\forex.shared\mt5\dll\
@SET DownloadedLibrariesFolder=C:\forex.shared\mt5\libraries\
@SET DownloadedEAFolder=C:\forex.shared\mt5\scripts\
rem ------------------------------------------------------------------------------------------------------------------------------
@SET DLLFolder=C:\Users\%userame%\AppData\Roaming\MetaQuotes\Terminal\%instance_id%\MQL5\Libraries\
@SET LibrariesFolder=C:\forex.mt5\libraries\
@SET EAFolder=C:\Users\%userame%\AppData\Roaming\MetaQuotes\Terminal\%instance_id%\MQL5\Experts\Advisors\
rem ------------------------------------------------------------------------------------------------------------------------------
mkdir %DLLFolder%
mkdir %LibrariesFolder%
mkdir %EAFolder%
rem ------------------------------------------------------------------------------------------------------------------------------
@ECHO ON
move %DownloadedDLLFolder%*.* %DLLFolder% 
move %DownloadedLibrariesFolder%*.* %LibrariesFolder% 
move %DownloadedEAFolder%*.* %EAFolder% 
pause
MAKE IT ON DEVELOPER COMPUTER:

xcopy "$(TargetDir)Common.Entities.dll" "C:\MT5\Libraries\" /Y /R

xcopy "$(TargetDir)Common.MetaQuotes.Mediator.dll" "C:\MT5\Libraries\" /Y /R


1) Post-Build action in project "MetaQuotes.Client.IndicatorToMediator":
xcopy "$(TargetDir)MetaQuotes.Client.Indicator.To.Mediator.dll" "C:\MT5\Libraries\" /Y /R
rem xcopy "C:\Users\andre\.nuget\packages\microsoft.bcl.asyncinterfaces\7.0.0\lib\netstandard2.0\Microsoft.Bcl.AsyncInterfaces.dll" "C:\MT5\Libraries\" /Y /R
rem xcopy "C:\Users\andre\.nuget\packages\pipemethodcalls\4.0.1\lib\netstandard2.0\PipeMethodCalls.dll" "C:\MT5\Libraries\" /Y /R
rem xcopy "C:\Users\andre\.nuget\packages\pipemethodcalls.netjson\3.0.0\lib\netstandard2.0\PipeMethodCalls.NetJson.dll" "C:\MT5\Libraries\" /Y /R
rem xcopy "C:\Users\andre\.nuget\packages\system.buffers\4.5.1\lib\netstandard2.0\System.Buffers.dll" "C:\MT5\Libraries\" /Y /R
rem xcopy "C:\Users\andre\.nuget\packages\system.memory\4.5.4\lib\netstandard2.0\System.Memory.dll" "C:\MT5\Libraries\" /Y /R
rem xcopy "C:\Users\andre\.nuget\packages\system.numerics.vectors\4.5.0\lib\netstandard2.0\System.Numerics.Vectors.dll" "C:\MT5\Libraries\" /Y /R
rem xcopy "C:\Users\andre\.nuget\packages\system.runtime.compilerservices.unsafe\5.0.0\lib\netstandard2.0\System.Runtime.CompilerServices.Unsafe.dll" "C:\MT5\Libraries\" /Y /R
rem xcopy "C:\Users\andre\.nuget\packages\system.text.encodings.web\7.0.0\lib\netstandard2.0\System.Text.Encodings.Web.dll" "C:\MT5\Libraries\" /Y /R
rem xcopy "C:\Users\andre\.nuget\packages\system.text.json\7.0.0\lib\netstandard2.0\System.Text.Json.dll" "C:\MT5\Libraries\" /Y /R
rem xcopy "C:\Users\andre\.nuget\packages\system.threading.tasks.extensions\4.5.4\lib\netstandard2.0\System.Threading.Tasks.Extensions.dll" "C:\MT5\Libraries\" /Y /R
rem xcopy "C:\MT5\Libraries\*.*" "\\UNIT1068\Users\Public\Downloads\MT5\Libraries\" /Y /R
xcopy "C:\MT5\Libraries\MetaQuotes.Client.Indicator.To.Mediator.dll" "C:\Users\andre\AppData\Roaming\MetaQuotes\Terminal\9B101088254A9C260A9790D5079A7B11\MQL5\Libraries\" /Y /R
xcopy "C:\Users\andre\AppData\Roaming\MetaQuotes\Terminal\9B101088254A9C260A9790D5079A7B11\MQL5\Indicators\Examples\Indicator.mq5" "D:\forex\fxSolution\MetaQuotes\MetaQuotes.MQL5\" /Y /R
rem xcopy "C:\Users\andre\AppData\Roaming\MetaQuotes\Terminal\9B101088254A9C260A9790D5079A7B11\MQL5\Indicators\Examples\Indicator.mq5" "\\UNIT1068\Users\Public\Downloads\MT5\" /Y /R
rem xcopy "D:\forex\fxSolution\MetaQuotes\MetaQuotes.MQL5\InstallOnUnit1068.bat" "\\UNIT1068\Users\Public\Downloads\" /Y /R
rem xcopy "D:\forex\fxSolution\MetaQuotes\MetaQuotes.MQL5\readMe.txt" "\\UNIT1068\Users\Public\Downloads\" /Y /R

2) Post-Build action in project "Mediator.Console":

rem xcopy "D:\forex\tickstory\tickstory.com.scripts\MS-SQL\01_CREATE_DATABASE_AND_TABLES.sql" "\\UNIT1068\Users\Public\Downloads\MS-SQL\" /Y /R
rem xcopy "D:\forex\tickstory\tickstory.com.scripts\MS-SQL\02_CREATE_TYPE_QuotationTableType.sql" "\\UNIT1068\Users\Public\Downloads\MS-SQL\" /Y /R
rem xcopy "D:\forex\tickstory\tickstory.com.scripts\MS-SQL\03_CREATE_PROCEDURE_InsertQuotations.sql" "\\UNIT1068\Users\Public\Downloads\MS-SQL\" /Y /R
rem xcopy "D:\forex\tickstory\tickstory.com.scripts\MS-SQL\04_CREATE_PROCEDURE_GetQuotationsByWeek.sql" "\\UNIT1068\Users\Public\Downloads\MS-SQL\" /Y /R
rem xcopy "D:\forex\tickstory\tickstory.com.scripts\MS-SQL\05_CREATE_PROCEDURE_GetQuotationsByWeekAndDay.sql" "\\UNIT1068\Users\Public\Downloads\MS-SQL\" /Y /R
rem xcopy "$(TargetDir)*.*" "\\UNIT1068\Users\Public\Downloads\Mediator" /Y /R

MAKE IT ON PRODUCTION COMPUTER:

1) run InstallOnUnit1068.bat as administrator
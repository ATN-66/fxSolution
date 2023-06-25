MAKE IT ON DEVELOPER COMPUTER:

1) Post-Build action in project "Common.Entities":

xcopy "$(TargetDir)Common.Entities.dll" "C:\forex.mt5\libraries\" /Y /R

2) Post-Build action in project "Common.MetaQuotes.Mediator":

xcopy "$(TargetDir)Common.MetaQuotes.Mediator.dll" "C:\forex.mt5\libraries\" /Y /R

3) Post-Build action in project "MetaQuotes.Client.IndicatorToMediator":

xcopy "$(TargetDir)MetaQuotes.Client.Indicator.To.Mediator.dll" "C:\forex.mt5\dll\" /Y /R
xcopy "$(TargetDir)MetaQuotes.Client.Indicator.To.Mediator.dll" "C:\Users\andre\AppData\Roaming\MetaQuotes\Terminal\9B101088254A9C260A9790D5079A7B11\MQL5\Libraries" /Y /R
xcopy "C:\forex.mt5\dll\*.*" "\\UNIT1068\forex.shared\mt5\dll\" /Y /R
rem ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------
rem check new packages!!!
xcopy "C:\Users\andre\.nuget\packages\microsoft.bcl.asyncinterfaces\8.0.0-preview.5.23280.8\lib\netstandard2.0\Microsoft.Bcl.AsyncInterfaces.dll" "C:\forex.mt5\libraries\" /Y /R
xcopy "C:\Users\andre\.nuget\packages\pipemethodcalls\4.0.1\lib\netstandard2.0\PipeMethodCalls.dll" "C:\forex.mt5\libraries\" /Y /R
xcopy "C:\Users\andre\.nuget\packages\pipemethodcalls.netjson\3.0.0\lib\netstandard2.0\PipeMethodCalls.NetJson.dll" "C:\forex.mt5\libraries\" /Y /R
xcopy "C:\Users\andre\.nuget\packages\system.buffers\4.5.1\lib\netstandard2.0\System.Buffers.dll" "C:\forex.mt5\libraries\" /Y /R
xcopy "C:\Users\andre\.nuget\packages\system.memory\4.5.5\lib\netstandard2.0\System.Memory.dll" "C:\forex.mt5\libraries\" /Y /R
xcopy "C:\Users\andre\.nuget\packages\system.numerics.vectors\4.5.0\lib\netstandard2.0\System.Numerics.Vectors.dll" "C:\forex.mt5\libraries\" /Y /R
xcopy "C:\Users\andre\.nuget\packages\system.runtime.compilerservices.unsafe\6.0.0\lib\netstandard2.0\System.Runtime.CompilerServices.Unsafe.dll" "C:\forex.mt5\libraries\" /Y /R
xcopy "C:\Users\andre\.nuget\packages\system.text.encodings.web\8.0.0-preview.5.23280.8\lib\netstandard2.0\System.Text.Encodings.Web.dll" "C:\forex.mt5\libraries\" /Y /R
xcopy "C:\Users\andre\.nuget\packages\system.text.json\8.0.0-preview.5.23280.8\lib\netstandard2.0\System.Text.Json.dll" "C:\forex.mt5\libraries\" /Y /R
xcopy "C:\Users\andre\.nuget\packages\system.threading.tasks.extensions\4.5.4\lib\netstandard2.0\System.Threading.Tasks.Extensions.dll" "C:\forex.mt5\libraries\" /Y /R

xcopy "C:\forex.mt5\libraries\*.*" "\\UNIT1068\forex.shared\mt5\libraries\" /Y /R
rem ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------
xcopy "C:\Users\andre\AppData\Roaming\MetaQuotes\Terminal\9B101088254A9C260A9790D5079A7B11\MQL5\Indicators\Examples\Indicator.mq5" "D:\forex\fxSolution\MetaQuotes\MetaQuotes.MQL5\" /Y /R
xcopy "C:\Users\andre\AppData\Roaming\MetaQuotes\Terminal\9B101088254A9C260A9790D5079A7B11\MQL5\Indicators\Examples\Indicator.mq5" "\\UNIT1068\forex.shared\mt5\scripts\" /Y /R
rem ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------
xcopy "D:\forex\fxSolution\MetaQuotes\MetaQuotes.MQL5\InstallOnUnit1068.bat" "\\UNIT1068\forex.shared\" /Y /R
xcopy "D:\forex\fxSolution\MetaQuotes\MetaQuotes.MQL5\readMe.txt" "\\UNIT1068\forex.shared\" /Y /R
rem ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------
xcopy "D:\forex\tickstory\tickstory.com.scripts\MS-SQL\MetaQuotes.MQL5\01_CREATE_DATABASE_AND_TABLES.sql"                 "\\UNIT1068\forex.shared\ms-sql" /Y /R
xcopy "D:\forex\tickstory\tickstory.com.scripts\MS-SQL\MetaQuotes.MQL5\02_CREATE_TYPE_QuotationTableType.sql"             "\\UNIT1068\forex.shared\ms-sql" /Y /R
xcopy "D:\forex\tickstory\tickstory.com.scripts\MS-SQL\MetaQuotes.MQL5\03_CREATE_PROCEDURE_InsertQuotations.sql"          "\\UNIT1068\forex.shared\ms-sql" /Y /R
xcopy "D:\forex\tickstory\tickstory.com.scripts\MS-SQL\MetaQuotes.MQL5\04_CREATE_PROCEDURE_GetQuotationsByWeekAndDay.sql" "\\UNIT1068\forex.shared\ms-sql" /Y /R
xcopy "D:\forex\tickstory\tickstory.com.scripts\MS-SQL\MetaQuotes.MQL5\05_CREATE_FUNCTION_GetLastBackupDate.sql"          "\\UNIT1068\forex.shared\ms-sql" /Y /R
xcopy "D:\forex\tickstory\tickstory.com.scripts\MS-SQL\MetaQuotes.MQL5\06_CREATE_PROCEDURE_BackupProviderDatabase.sql"    "\\UNIT1068\forex.shared\ms-sql" /Y /R

MAKE IT ON PRODUCTION COMPUTER:

1) run InstallOnUnit1068.bat as administrator
2) install .sql scripts
3) change Indicator.mq5 to production

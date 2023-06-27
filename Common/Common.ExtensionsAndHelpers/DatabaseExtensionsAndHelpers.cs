/*+------------------------------------------------------------------+
  |                                       Common.ExtensionsAndHelpers|
  |                                  DatabaseExtensionsAndHelpers.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;

namespace Common.ExtensionsAndHelpers
{
    public static class DatabaseExtensionsAndHelpers
    {
        public static string GetServerName()
        {
            return @"Server=" + Environment.MachineName + @"\SQLEXPRESS";
        }
        public static string GetDatabaseName(int yearNumber, int weekNumber, Provider provider) => $"{yearNumber}.{DateTimeExtensionsAndHelpers.Quarter(weekNumber)}.{provider.ToString().ToLower()}";
        public static string GetTableName(int weekNumber) => $"week{weekNumber:00}";
    }
}
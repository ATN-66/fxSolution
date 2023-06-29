using Microsoft.Extensions.Logging;

namespace Common.ExtensionsAndHelpers;

public static class LogExceptionHelper
{
    public static void LogException(ILogger logger, Exception exception, string methodName, string? message = null)
    {
        if (!string.IsNullOrEmpty(message))
        {
            logger.LogError("{Message}", message);
        }

        logger.LogError("Method Name: {methodName}", methodName);
        logger.LogError("Exception (1st level): {exception.Message}", exception.Message);
        logger.LogError("Exception (2nd level):{exception.Message}", exception.InnerException?.Message);
        logger.LogError("{ExceptionType}: {exception.Message}\n{StackTrace}", exception.GetType(), exception.Message, exception.StackTrace);//todo
    }
}
using Microsoft.Extensions.Logging;

namespace Common.ExtensionsAndHelpers;

public static class LogExceptionHelper
{
    public static void LogException(ILogger logger, Exception exception, string? message = null)
    {
        if (!string.IsNullOrEmpty(message))
        {
            logger.LogError("Custom Message: {Message}", message);
        }

        logger.LogError("Exception Details: ");

        var currentException = exception;
        var exceptionLevel = 1;
        while (currentException != null)
        {
            logger.LogError("Exception (level {Level}): {exceptionMessage}", exceptionLevel++, currentException.Message);
            currentException = currentException.InnerException;
        }

        logger.LogError("<-------- StackTrace -------->");
        logger.LogError("{ExceptionType}: {exceptionMessage}\n{StackTrace}", exception.GetType(), exception.Message, exception.StackTrace);
    }
}
using Microsoft.Extensions.Logging;

namespace Terminal.WinUI3.Helpers;

internal static class LogExceptionHelper
{
    public static void LogException(ILogger logger, Exception exception, string? message = null)
    {
        if (!string.IsNullOrEmpty(message))
        {
            logger.LogError("{Message}", message);
        }

        logger.LogError("{exception.Message}", exception.Message);
        logger.LogError("{exception.Message}", exception.InnerException?.Message);
        logger.LogError("{ExceptionType}: {exception.Message}\n{StackTrace}", exception.GetType(), exception.Message, exception.StackTrace);
    }
}
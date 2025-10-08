namespace CityInfoApi.Common
{
    public static class AppLogger
    {
        public static void LogActionStart(ILogger logger, string action, object? context = null)
        {
            logger.LogInformation("Starting action {Action} with context {@Context}", action, context);
        }

        public static void LogActionSuccess(ILogger logger, string action, object? context = null)
        {
            logger.LogInformation("Action {Action} completed successfully. {@Context}", action, context);
        }

        public static void LogWarning(ILogger logger, string message, object? context = null)
        {
            logger.LogWarning("{Message}. {@Context}", message, context);
        }

        public static void LogError(ILogger logger, string message, Exception? ex = null)
        {
            logger.LogError(ex, "{Message}", message);
        }
    }
}

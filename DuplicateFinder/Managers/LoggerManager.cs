using System.Threading.Tasks;
using ITCC.Logging.Core;
using ITCC.UI.Loggers;

namespace DuplicateFinder.Managers
{
    public static class LoggerManager
    {
        public static void StartLoggers()
        {
            Logger.Level = LogLevel.Trace;
            ObservableLogger = new ObservableLogger(10000, App.RunOnUiThreadAsync) { Level = Logger.Level };
            Logger.RegisterReceiver(ObservableLogger, true);
        }

        public static Task FinalizeLoggersAsync()
        {
            App.LogMessage(LogLevel.Info, "Application is shutting down");
            return Logger.FlushAllAsync();
        }

        public static ObservableLogger ObservableLogger { get; private set; }
    }
}

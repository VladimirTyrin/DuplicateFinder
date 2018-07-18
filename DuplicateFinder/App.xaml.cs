#define AUTO_LOG_WINDOW

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using DuplicateFinder.Managers;
using DuplicateFinder.UI.Windows;
using ITCC.Logging.Core;
using ITCC.WPF.Windows;

namespace DuplicateFinder
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    // ReSharper disable once RedundantExtendsListEntry
    public partial class App : Application
    {
        public static Window GetMainWindow() => Current.Windows.OfType<MainWindow>().First();

        public static async Task RunOnUiThreadAsync(Action action) => await Current.Dispatcher.InvokeAsync(action);

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                LoggerManager.StartLoggers();
                LogMessage(LogLevel.Info, "Logging started");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + ex.InnerException?.Message,
                    "Startup error", MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown();
            }

#if DEBUG && AUTO_LOG_WINDOW
            var logWindow = new LogWindow(LoggerManager.ObservableLogger);
            logWindow.Show();
#endif
            LogMessage(LogLevel.Info, "Application is ready");
        }

        private async void App_OnExit(object sender, ExitEventArgs e)
        {
            await LoggerManager.FinalizeLoggersAsync();
        }

        private async void App_OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs args)
        {
            LogException(LogLevel.Critical, args.Exception);
            await Logger.FlushAllAsync();
#if DEBUG
            args.Handled = true;
#else
            Current.Shutdown();
#endif
        }

        public static void LogMessage(LogLevel level, string message) => Logger.LogEntry("APPLICATION", level, message);

        private static void LogException(LogLevel level, Exception exception) => Logger.LogException("APPLICATION", level, exception);
    }
}

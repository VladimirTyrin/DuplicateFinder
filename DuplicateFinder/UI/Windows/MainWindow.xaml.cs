using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using DuplicateFinder.Helpers;
using DuplicateFinder.Managers;
using DuplicateFinder.Search;
using DuplicateFinder.Settings;
using ITCC.WPF.Windows;

namespace DuplicateFinder.UI.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    // ReSharper disable once RedundantExtendsListEntry
    public partial class MainWindow : Window, IProgressHandler
    {
        private static readonly Random Random = new Random();
        private static bool _isRunning;
        private Timer _timer;
        private Stopwatch _stopwatch;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void SettingsItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (_isRunning)
            {
                MessageBox.Show("Search is in progress!");
                return;
            }
            var settingsWindow = new SettingsWindow();
            UiHelper.CentredWindow(settingsWindow).ShowDialog();
        }

        private void ResultsItem_OnClick(object sender, RoutedEventArgs e)
        {
            var directory = GetResultsDirectory();
            Process.Start("explorer.exe", directory);
        }

        private void ExitItem_OnClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        public async Task ReportCurrentAsync(string message, int foldersQueued, int foldersProcessed, int filesProcessed)
        {
            if (Random.Next() % SearchSettings.Instance.UpdateInterval != 0)
                return;

            await SetStateAsync(message);
            await App.RunOnUiThreadAsync(() =>
            {
                DirectoriesQueuedLabel.Content =    $"Directories queued:    {foldersQueued}";
                DirectoriesProcessedLabel.Content = $"Directories processed: {foldersProcessed}";
                FilesProcessedLabel.Content = $"Files processed:       {filesProcessed}";
            });
        }

        public Task ReportStateAsync(string message)
        {
            return SetStateAsync(message);
        }

        public Task ReportCompletedAsync()
        {
            return App.RunOnUiThreadAsync(() => SetIsRunning(false));
        }

        private Task SetStateAsync(string message)
        {
            return App.RunOnUiThreadAsync(() => ProgressLabel.Content = message);
        }

        private async void StartButton_OnClick(object sender, RoutedEventArgs e)
        {
            SetIsRunning(true);
            var result = await SearchManager.SearchForDuplicatesAsync(this);
            if (result == null)
            {
                await SetStateAsync("Search failed!");
                return;
            }
            if (result.Canceled)
            {
                await SetStateAsync("Search canceled");
                return;
            }
            var path = GetTargetFilePath();
            await SetStateAsync($"Saving result to {path}");
            if (await SearchResultHelper.SaveResultAsync(path, result))
            {
                DirectoriesQueuedLabel.Content = $"Directories queued:    0";
                await SetStateAsync("Done!");
                Process.Start(path);
            }
            else
            {
                await SetStateAsync("Failed to save result");
            }

        }

        private static string GetTargetFilePath()
        {
            var appDataDir = GetResultsDirectory();
            var fileName = Path.Combine(appDataDir, $"Result_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt");
            return fileName;
        }

        private static string GetResultsDirectory()
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DuplicateFinder");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            return path;
        }

        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            SearchManager.CancelSearch();
        }

        private void SetIsRunning(bool isRunning)
        {
            StartButton.IsEnabled = !isRunning;
            CancelButton.IsEnabled = isRunning;
            _isRunning = isRunning;
            if (isRunning)
            {
                _timer = new Timer
                {
                    Enabled = true,
                    Interval = 150
                };
                _stopwatch = Stopwatch.StartNew();
                _timer.Elapsed += async (sender, args) => await App.RunOnUiThreadAsync(() =>
                {
                    TimeElapseddLabel.Content = $"Time elapsed: {_stopwatch.Elapsed}";
                });
            }
            else
            {
                _timer?.Stop();
                _stopwatch?.Stop();
            }
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            Application.Current.Windows.OfType<LogWindow>().FirstOrDefault()?.Close();
        }
    }
}

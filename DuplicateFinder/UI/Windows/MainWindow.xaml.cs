using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using DuplicateFinder.Helpers;
using DuplicateFinder.Managers;
using DuplicateFinder.Search;
using ITCC.WPF.Windows;

namespace DuplicateFinder.UI.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    // ReSharper disable once RedundantExtendsListEntry
    public partial class MainWindow : Window, IProgressHandler
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void SettingsItem_OnClick(object sender, RoutedEventArgs e)
        {
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

        public Task ReportCurrentAsync(string message)
        {
            return App.RunOnUiThreadAsync(() => ProgressLabel.Content = message);
        }

        public Task ReportCompletedAsync()
        {
            return App.RunOnUiThreadAsync(() => SetIsRunning(false));
        }

        private async void StartButton_OnClick(object sender, RoutedEventArgs e)
        {
            SetIsRunning(true);
            var result = await SearchManager.SearchForDuplicatesAsync(this);
            if (result == null)
            {
                await ReportCurrentAsync("Search failed!");
                return;
            }
            var path = GetTargetFilePath();
            await ReportCurrentAsync($"Saving result to {path}");
            if (await SearchResultHelper.SaveResultAsync(path, result))
            {
                await ReportCurrentAsync("Done!");
            }
            else
            {
                await ReportCurrentAsync("Failed to save result");
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
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            Application.Current.Windows.OfType<LogWindow>().FirstOrDefault()?.Close();
        }
    }
}

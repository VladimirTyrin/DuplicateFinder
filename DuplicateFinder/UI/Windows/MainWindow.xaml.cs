using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using DuplicateFinder.Helpers;
using DuplicateFinder.Managers;
using DuplicateFinder.Search;

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
            return App.RunOnUiThreadAsync(() => StartButton.IsEnabled = true);
        }

        private async void StartButton_OnClick(object sender, RoutedEventArgs e)
        {
            StartButton.IsEnabled = false;
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
            var appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DuplicateFinder");
            var fileName = Path.Combine(appDataDir, $"Result_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt");
            return fileName;
        }
    }
}

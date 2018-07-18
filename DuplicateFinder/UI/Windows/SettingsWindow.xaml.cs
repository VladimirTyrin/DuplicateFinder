using System.Windows;

namespace DuplicateFinder.UI.Windows
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
        }

        private void SaveAndCloseButton_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

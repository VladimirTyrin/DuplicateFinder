using System.Windows;

namespace DuplicateFinder.Helpers
{
    public class UiHelper
    {
        public static TWindow CentredWindow<TWindow>(TWindow window, WindowStartupLocation windowStartupLocation = WindowStartupLocation.CenterOwner) where TWindow : Window
        {
            window.Owner = App.GetMainWindow();
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            return window;
        }
    }
}

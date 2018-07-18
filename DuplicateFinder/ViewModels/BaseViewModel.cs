using System.ComponentModel;
using System.Runtime.CompilerServices;
using ITCC.Logging.Core;
using JetBrains.Annotations;

namespace DuplicateFinder.ViewModels
{
    public class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            Logger.LogEntry("VIEWMODEL", LogLevel.Trace, $"{GetType().Name}::{propertyName} value changed");
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void OnExplicitPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

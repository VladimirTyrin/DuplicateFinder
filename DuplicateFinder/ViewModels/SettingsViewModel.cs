namespace DuplicateFinder.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        public bool EntireMachine
        {
            get => Settings.SearchSettings.Instance.EntireMachine;
            set
            {
                var current = Settings.SearchSettings.Instance.EntireMachine;
                if (current == value)
                    return;

                Settings.SearchSettings.Instance.EntireMachine = value;
                OnPropertyChanged();
            }
        }

        public bool IgnoreExtensions
        {
            get => Settings.SearchSettings.Instance.IgnoreExtensions;
            set
            {
                var current = Settings.SearchSettings.Instance.IgnoreExtensions;
                if (current == value)
                    return;

                Settings.SearchSettings.Instance.IgnoreExtensions = value;
                OnPropertyChanged();
            }
        }

        public string ThreadCount
        {
            get => Settings.SearchSettings.Instance.ThreadCount.ToString();
            set
            {
                var current = Settings.SearchSettings.Instance.ThreadCount.ToString();
                if (current == value)
                    return;

                if (! int.TryParse(value, out var newValue))
                    return;

                Settings.SearchSettings.Instance.ThreadCount = newValue;
                OnPropertyChanged();
            }
        }

        public string UpdateInterval
        {
            get => Settings.SearchSettings.Instance.UpdateInterval.ToString();
            set
            {
                var current = Settings.SearchSettings.Instance.UpdateInterval.ToString();
                if (current == value)
                    return;

                if (!int.TryParse(value, out var newValue))
                    return;

                Settings.SearchSettings.Instance.UpdateInterval = newValue;
                OnPropertyChanged();
            }
        }

        public string ExtensionsToUse
        {
            get => Settings.SearchSettings.Instance.ExtensionsToUse;
            set
            {
                var current = Settings.SearchSettings.Instance.ExtensionsToUse;
                if (current == value)
                    return;

                Settings.SearchSettings.Instance.ExtensionsToUse = value;
                OnPropertyChanged();
            }
        }
    }
}

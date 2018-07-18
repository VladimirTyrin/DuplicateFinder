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
    }
}

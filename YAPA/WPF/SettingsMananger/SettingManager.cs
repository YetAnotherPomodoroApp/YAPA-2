using System.ComponentModel;
using System.Runtime.CompilerServices;
using YAPA.Shared.Contracts;

namespace YAPA.WPF.SettingsMananger
{
    public class SettingManager : ISettingManager
    {
        private bool _restartNeeded;
        private string _newVersion;

        public bool RestartNeeded
        {
            get => _restartNeeded;
            set
            {
                _restartNeeded = value;
                OnPropertyChanged();
            }
        }

        public string NewVersion
        {
            get => _newVersion;
            set
            {
                _newVersion = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

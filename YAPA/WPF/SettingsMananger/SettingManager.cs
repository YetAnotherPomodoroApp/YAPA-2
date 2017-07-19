using System.ComponentModel;
using System.Runtime.CompilerServices;
using YAPA.Shared.Contracts;

namespace YAPA.WPF.SettingsMananger
{
    public class SettingManager : ISettingManager
    {
        private bool _restartNeeded;

        public bool RestartNeeded
        {
            get { return _restartNeeded; }
            set
            {
                _restartNeeded = value;
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

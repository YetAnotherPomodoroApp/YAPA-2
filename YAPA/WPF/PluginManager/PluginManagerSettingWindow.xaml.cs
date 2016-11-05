using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using YAPA.Contracts;
using YAPA.Shared;

namespace YAPA.WPF
{
    public partial class PluginManagerSettingWindow : UserControl
    {
        private readonly IPluginManager _plugins;
        private readonly PluginManagerSettings _settings;
        private readonly ISettings _globalSettings;
        private readonly ISettingManager _settingManager;
        public List<PluginViewModel> Plugins { get; set; }
        public ICommand TogglePlugin { get; set; }
        private List<string> _disabledPlugins;

        public PluginManagerSettingWindow(IPluginManager plugins, PluginManagerSettings settings, ISettings globalSettings, ISettingManager settingManager)
        {
            _plugins = plugins;
            _settings = settings;
            _settings.DeferChanges();

            _globalSettings = globalSettings;
            _settingManager = settingManager;
            _globalSettings.PropertyChanged += _globalSettings_PropertyChanged;

            _disabledPlugins = _settings.DisabledPlugins;

            Plugins = new List<PluginViewModel>();
            foreach (var pluginMeta in _plugins.CustomPlugins)
            {
                Plugins.Add(new PluginViewModel
                {
                    Title = pluginMeta.Title,
                    Disabled = _disabledPlugins.Contains(pluginMeta.Title)
                });
            }

            InitializeComponent();

            PluginList.ItemsSource = Plugins;
            DataContext = this;
        }

        private void _globalSettings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == $"{nameof(PluginManager)}.{nameof(_settings.DisabledPlugins)}")
            {
                _settingManager.RestartNeeded = true;
            }
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            var context = ((CheckBox)sender).DataContext as PluginViewModel;
            if (context.Disabled)
            {
                _disabledPlugins.Add(context.Title);
            }
            else
            {
                var existing = _disabledPlugins.FirstOrDefault(x => x == context.Title);
                if (existing != null)
                {
                    _disabledPlugins.Remove(existing);
                }
            }

            if (_disabledPlugins.Count == 0)
            {
                _settings.DisabledPlugins = null;
            }
            else
            {
                _settings.DisabledPlugins = _disabledPlugins;
            }
        }
    }

    public class PluginViewModel
    {
        public string Title { get; set; }
        public bool Disabled { get; set; }
    }

}

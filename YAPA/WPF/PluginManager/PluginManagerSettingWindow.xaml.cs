using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using YAPA.Shared.Contracts;

namespace YAPA.WPF.PluginManager
{
    public partial class PluginManagerSettingWindow : UserControl
    {
        private readonly PluginManagerSettings _settings;
        private readonly ISettingManager _settingManager;
        public List<PluginViewModel> Plugins { get; set; }
        public ICommand TogglePlugin { get; set; }
        private readonly List<string> _enabledPlugins;

        public PluginManagerSettingWindow(IPluginManager plugins, PluginManagerSettings settings, ISettings globalSettings, ISettingManager settingManager)
        {
            _settings = settings;
            _settings.DeferChanges();

            _settingManager = settingManager;
            globalSettings.PropertyChanged += _globalSettings_PropertyChanged;

            _enabledPlugins = _settings.EnabledPlugins;

            Plugins = new List<PluginViewModel>();
            foreach (var pluginMeta in plugins.CustomPlugins)
            {
                Plugins.Add(new PluginViewModel
                {
                    Title = pluginMeta.Title,
                    Enabled = _enabledPlugins.Contains(pluginMeta.Id),
                    Id = nameof(pluginMeta.Plugin)
                });
            }

            InitializeComponent();

            PluginList.ItemsSource = Plugins;
            DataContext = this;
        }

        private void _globalSettings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == $"{nameof(PluginManager)}.{nameof(_settings.EnabledPlugins)}")
            {
                _settingManager.RestartNeeded = true;
            }
        }

        private void PluginChanged(object sender, RoutedEventArgs e)
        {
            var context = ((CheckBox)sender).DataContext as PluginViewModel;
            if (context == null)
            {
                return;
            }
            if (context.Enabled)
            {
                var existing = _enabledPlugins.FirstOrDefault(x => x == context.Id);
                if (string.IsNullOrEmpty(existing))
                {
                    _enabledPlugins.Add(context.Id);
                }
            }
            else
            {
                var existing = _enabledPlugins.FirstOrDefault(x => x == context.Id);
                if (existing != null)
                {
                    _enabledPlugins.Remove(existing);
                }
            }

            _settings.EnabledPlugins = _enabledPlugins.Any() ? _enabledPlugins : null;
        }
    }

    public class PluginViewModel
    {
        public string Title { get; set; }
        public string Id { get; set; }
        public bool Enabled { get; set; }
    }

}

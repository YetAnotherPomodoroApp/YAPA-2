using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using YAPA.Avalonia.Infrastructure;
using YAPA.Shared.Contracts;

namespace YAPA.Avalonia.Settings
{
    public partial class PluginManagerSettingWindow : UserControl
    {
        private readonly PluginManagerSettings _settings;
        private readonly ISettingManager _settingManager;
        private readonly List<string> _enabledPlugins;

        public List<PluginViewModel> Plugins { get; set; }

        public PluginManagerSettingWindow()
        {
            InitializeComponent();
        }

        public PluginManagerSettingWindow(IPluginManager plugins, PluginManagerSettings settings, ISettings globalSettings, ISettingManager settingManager)
        {
            _settings = settings;
            _settings.DeferChanges();
            _settingManager = settingManager;
            globalSettings.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == $"{nameof(PluginManager)}.{nameof(_settings.EnabledPlugins)}")
                {
                    _settingManager.RestartNeeded = true;
                }
            };

            _enabledPlugins = _settings.EnabledPlugins;

            Plugins = plugins.CustomPlugins
                .Select(pluginMeta => new PluginViewModel
                {
                    Title = pluginMeta.Title,
                    Enabled = _enabledPlugins.Contains(pluginMeta.Id),
                    Id = pluginMeta.Id
                })
                .ToList();

            InitializeComponent();
            PluginList.ItemsSource = Plugins;
            DataContext = this;
        }

        private void PluginChanged(object sender, RoutedEventArgs e)
        {
            var checkBox = (CheckBox)sender;
            if (!(checkBox.DataContext is PluginViewModel context) || _enabledPlugins == null)
            {
                return;
            }
            // Avalonia raises this before the binding has updated the item, so read the box
            context.Enabled = checkBox.IsChecked == true;

            var existing = _enabledPlugins.FirstOrDefault(x => x == context.Id);
            if (context.Enabled)
            {
                if (string.IsNullOrEmpty(existing))
                {
                    _enabledPlugins.Add(context.Id);
                }
            }
            else if (existing != null)
            {
                _enabledPlugins.Remove(existing);
            }

            // A real change only: the first event comes from the initial binding
            if (!_settings.EnabledPlugins.OrderBy(x => x).SequenceEqual(_enabledPlugins.OrderBy(x => x)))
            {
                _settings.EnabledPlugins = _enabledPlugins.Any() ? _enabledPlugins.ToList() : null;
            }
        }
    }

    public class PluginViewModel
    {
        public string Title { get; set; }
        public string Id { get; set; }
        public bool Enabled { get; set; }
    }
}

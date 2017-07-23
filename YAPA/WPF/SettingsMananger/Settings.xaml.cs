using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using YAPA.Commands;
using YAPA.Shared.Contracts;
using YAPA.WPF.PluginManager;

namespace YAPA.WPF.SettingsMananger
{
    public partial class Settings : Window
    {
        private readonly ISettings _settings;
        private readonly ISettingManager _mananger;
        private readonly IPluginManager _pluginManager;
        private readonly IDependencyInjector _container;
        public ICommand SaveCommand { get; set; }
        public ICommand CancelCommand { get; set; }
        private UserControl _settingPage = null;

        public Settings(ISettings settings, ISettingManager mananger, IPluginManager pluginManager, IDependencyInjector container)
        {
            _settings = settings;
            _mananger = mananger;
            _pluginManager = pluginManager;
            _container = container;
            _mananger.PropertyChanged += _mananger_PropertyChanged;

            SaveCommand = new SaveSettingsCommand(_settings);
            CancelCommand = new CancelSettingsCommand(this, _settings);


            DataContext = this;
            InitializeComponent();

            SettingsTree.SelectedItemChanged += SettingsTree_SelectedItemChanged;


            foreach (var rootSetting in _pluginManager.BuiltInPlugins.Where(x => x.SettingEditWindow != null && !x.IsHidden()))
            {
                var builtinPlugin = new TreeViewItem
                {
                    Header = rootSetting.Title,
                    IsSelected = SettingsTree.Items.IsEmpty
                };

                SettingsTree.Items.Add(builtinPlugin);
            }


            var pluginsTree = new TreeViewItem { Header = "Plugins" };

            foreach (var customSettings in _pluginManager.ActivePlugins.Where(x => x.SettingEditWindow != null))
            {
                pluginsTree.Items.Add(new TreeViewItem() { Header = customSettings.Title });
            }

            SettingsTree.Items.Add(pluginsTree);

            var aboutPage = new TreeViewItem { Header = "About" };
            SettingsTree.Items.Add(aboutPage);

            UpdateNotificationMessage();

            Loaded += Settings_Loaded;
        }

        private void UpdateNotificationMessage()
        {
            RestartAppNotification.Visibility = _mananger.RestartNeeded ? Visibility.Visible : Visibility.Collapsed;

            var settingsChanged = "Restart application to apply changes";
            var updatesInstalled = "Restart application to apply updates";

            var message = settingsChanged;

            if (!string.IsNullOrEmpty(_mananger.NewVersion))
            {
                message = updatesInstalled;
            }

            NotificationMessage.Text = message;
        }

        private void Settings_Loaded(object sender, RoutedEventArgs e)
        {
            var assembly = Assembly.GetExecutingAssembly();

            Version.Text = assembly.GetName().Version.ToString(3);
        }

        private void _mananger_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_mananger.RestartNeeded)
                || e.PropertyName == nameof(_mananger.NewVersion))
            {
                UpdateNotificationMessage();
            }
        }

        private void SettingsTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (_settingPage != null)
            {
                SettingGrid.Children.Remove(_settingPage);
            }

            _settingPage = null;

            var treeItem = e.NewValue as TreeViewItem;
            if (treeItem == null)
            {
                return;
            }
            UserControl child = null;

            if (treeItem.Header.ToString() == "Plugins")
            {
                _settingPage = (UserControl)_container.Resolve(typeof(PluginManager.PluginManagerSettingWindow));
            }
            else
            if (treeItem.Header.ToString() == "About")
            {
                _settingPage = (UserControl)_container.Resolve(typeof(AboutPage));
            }
            else
            {
                _settingPage = (UserControl)_container.Resolve(_pluginManager.Plugins.First(x => x.Title == treeItem.Header.ToString()).SettingEditWindow);
            }
            if (_settingPage != null)
            {
                SettingGrid.Children.Add(_settingPage);
                Grid.SetColumn(_settingPage, 1);
                Grid.SetRow(_settingPage, 0);
                _settingPage.Margin = new Thickness(10, 20, 10, 10);
            }
        }

        private void Settings_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                base.OnMouseLeftButtonDown(e);
                DragMove();
                e.Handled = true;
            }
            catch
            {
            }
        }
    }
}

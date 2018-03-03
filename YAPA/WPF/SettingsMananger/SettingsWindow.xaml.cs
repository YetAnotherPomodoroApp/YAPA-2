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
    public partial class SettingsWindow
    {
        private readonly ISettings _settings;
        private readonly ISettingManager _mananger;
        private readonly IPluginManager _pluginManager;
        private readonly IDependencyInjector _container;
        private readonly IEnvironment _environment;
        public ICommand SaveCommand { get; set; }
        public ICommand CancelCommand { get; set; }
        private UserControl _settingPage = null;

        public SettingsWindow(ISettings settings, ISettingManager mananger, IPluginManager pluginManager, IDependencyInjector container, IEnvironment environment)
        {
            _settings = settings;
            _mananger = mananger;
            _pluginManager = pluginManager;
            _container = container;
            _environment = environment;
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
                    IsSelected = SettingsTree.Items.IsEmpty,
                    Tag = _container.Resolve(rootSetting.SettingEditWindow)
                };

                SettingsTree.Items.Add(builtinPlugin);
            }

            var pluginsTree = new TreeViewItem
            {
                Header = "Plugins",
                Tag = _container.Resolve(typeof(PluginManagerSettingWindow))
            };

            foreach (var customSettings in _pluginManager.ActivePlugins.Where(x => x.SettingEditWindow != null))
            {
                pluginsTree.Items.Add(new TreeViewItem
                {
                    Header = customSettings.Title,
                    Tag = _container.Resolve(customSettings.SettingEditWindow)
                });
            }

            SettingsTree.Items.Add(pluginsTree);

            var aboutPage = new TreeViewItem { Header = "About" };
            SettingsTree.Items.Add(aboutPage);

            UpdateNotificationMessage();

            Loaded += Settings_Loaded;
            IsVisibleChanged += SettingsWindow_IsVisibleChanged;
        }

        private void SettingsWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (Visibility == Visibility.Visible)
            {
                RefreshSelectedMenuItem();
            }
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

            if (_environment.PreRelease())
            {
                Version.Text += " pre-release";
            }
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
            RefreshSelectedMenuItem();
        }

        private void RefreshSelectedMenuItem()
        {
            if (_settingPage != null)
            {
                SettingGrid.Children.Remove(_settingPage);
            }

            _settingPage = null;

            if (!(SettingsTree.SelectedItem is TreeViewItem treeItem))
            {
                return;
            }

            if (treeItem.Tag is IPluginSettingWindow)
            {
                ((IPluginSettingWindow)treeItem.Tag).Refresh();
            }

            if (treeItem.Tag is UserControl)
            {
                _settingPage = (UserControl)treeItem.Tag;
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
                OnMouseLeftButtonDown(e);
                DragMove();
                e.Handled = true;
            }
            catch
            {
            }
        }
    }
}

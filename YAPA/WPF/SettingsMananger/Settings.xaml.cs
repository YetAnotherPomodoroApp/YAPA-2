using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using YAPA.Contracts;
using YAPA.Shared;
using YAPA.WPF;

namespace YAPA
{
    public partial class Settings : Window
    {
        private readonly ISettings _settings;
        private readonly ISettingManager _mananger;
        private readonly IPluginManager _pluginManager;
        private readonly IDependencyInjector _container;
        public ICommand SaveCommand { get; set; }
        public ICommand CancelCommand { get; set; }

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

            RestartAppNotification.Visibility = _mananger.RestartNeeded ? Visibility.Visible : Visibility.Collapsed;
        }

        private void _mananger_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_mananger.RestartNeeded))
            {
                RestartAppNotification.Visibility = _mananger.RestartNeeded ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void SettingsTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            SettingPage.Children.Clear();

            var treeItem = e.NewValue as TreeViewItem;
            if (treeItem == null)
            {
                return;
            }
            UserControl child = null;

            if (treeItem.Header.ToString() == "Plugins")
            {
                child = (UserControl)_container.Resolve(typeof(PluginManagerSettingWindow));
            }
            else
            if (treeItem.Header.ToString() == "About")
            {
                child = new AboutPage();
            }
            else
            {
                child = (UserControl)_container.Resolve(_pluginManager.Plugins.First(x => x.Title == treeItem.Header.ToString()).SettingEditWindow);
            }

            SettingPage.Children.Add(child);
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

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using YAPA.Contracts;

namespace YAPA
{

    public partial class Settings : Window
    {
        private readonly ISettings _settings;
        private readonly ISettingManager _mananger;
        public ICommand SaveCommand { get; set; }
        public ICommand CancelCommand { get; set; }

        public Settings(ISettings settings, ISettingManager mananger)
        {
            _settings = settings;
            _mananger = mananger;

            SaveCommand = new SaveSettingsCommand(this, settings);
            CancelCommand = new CancelSettingsCommand(this, settings);


            DataContext = this;
            InitializeComponent();

            SettingsTree.SelectedItemChanged += SettingsTree_SelectedItemChanged;

            foreach (var rootSetting in _mananger.GetRootSettings())
            {
                var pluginsTree = new TreeViewItem { Header = rootSetting };

                if (rootSetting == "Plugins")
                {
                    foreach (var plugin in _mananger.GetPlugins())
                    {
                        pluginsTree.Items.Add(new TreeViewItem() { Header = plugin });
                    }
                }

                SettingsTree.Items.Add(pluginsTree);
            }

            RestartAppNotification.Visibility = _mananger.RestartNeeded ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SettingsTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            SettingPage.Children.Clear();

            var treeItem = e.NewValue as TreeViewItem;
            if (treeItem == null)
            {
                return;
            }

            SettingPage.Children.Add(_mananger.GetPageFor((string)treeItem.Header));
        }
    }
}

using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using YAPA.Contracts;

namespace YAPA
{

    public partial class Settings : Window
    {
        private readonly ISettings _settings;
        private readonly IPluginManager _plugins;
        public ICommand SaveCommand { get; set; }
        public ICommand CancelCommand { get; set; }

        public Settings(ISettings settings, IPluginManager plugins)
        {
            _settings = settings;
            _plugins = plugins;

            SaveCommand = new SaveSettingsCommand(this, settings);
            CancelCommand = new CancelSettingsCommand(this, settings);


            DataContext = this;
            InitializeComponent();

            SettingsTree.SelectedItemChanged += SettingsTree_SelectedItemChanged;

            var pluginsTree = new TreeViewItem { Header = "Plugins" };
            foreach (var plugin in _plugins.Plugins)
            {
                pluginsTree.Items.Add(new TreeViewItem() { Header = plugin.Title });
            }

            SettingsTree.Items.Add(pluginsTree);

        }

        private void SettingsTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            SettingPage.Children.Clear();

            var treeItem = e.NewValue as TreeViewItem;
            if (treeItem == null)
            {
                return;
            }

            var plugin = _plugins.Plugins.FirstOrDefault(x => Equals(x.Title, (string)treeItem.Header));
            if (plugin?.SettingEditWindow == null)
            {
                return;
            }

            var editPage = _plugins.ResolveSettingWindow(plugin);

            SettingPage.Children.Add((UserControl)editPage);
        }
    }
}

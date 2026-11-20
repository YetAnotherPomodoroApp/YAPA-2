using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using YAPA.Avalonia.Infrastructure;
using YAPA.Shared.Contracts;

namespace YAPA.Avalonia.Settings
{
    public partial class SettingsWindow : Window
    {
        private readonly ISettings _settings;
        private readonly ISettingManager _mananger;
        private readonly IPluginManager _pluginManager;
        private readonly IDependencyInjector _container;
        private readonly IEnvironment _environment;
        private Control _settingPage;

        public ICommand SaveCommand { get; set; }
        public ICommand CancelCommand { get; set; }

        public SettingsWindow()
        {
            InitializeComponent();
        }

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

            SettingsTree.SelectionChanged += (s, e) => RefreshSelectedMenuItem();

            TreeViewItem first = null;
            foreach (var rootSetting in _pluginManager.BuiltInPlugins.Where(x => x.SettingEditWindow != null && !x.IsHidden()))
            {
                var builtinPlugin = MenuItem(rootSetting.Title, rootSetting.SettingEditWindow);
                first ??= builtinPlugin;
                SettingsTree.Items.Add(builtinPlugin);
            }

            var pluginsTree = MenuItem("Plugins", typeof(PluginManagerSettingWindow));
            foreach (var customSettings in _pluginManager.ActivePlugins.Where(x => x.SettingEditWindow != null))
            {
                pluginsTree.Items.Add(MenuItem(customSettings.Title, customSettings.SettingEditWindow));
            }
            SettingsTree.Items.Add(pluginsTree);

            SettingsTree.Items.Add(MenuItem("About", typeof(AboutPage)));

            UpdateNotificationMessage();

            var version = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(2, 0, 0);
            Version.Text = version.ToString(3);
            if (_environment.PreRelease())
            {
                Version.Text += " pre-release";
            }

            PointerPressed += Settings_OnMouseLeftButtonDown;
            Opened += (s, e) => RefreshSelectedMenuItem();
            Closing += SettingsWindow_Closing;

            SettingsTree.SelectedItem = first;
#if DEBUG
            Opened += (s, e) => Snapshots.RenderPages(this, SettingsTree);
#endif
        }

        private TreeViewItem MenuItem(string title, Type page)
        {
            // The page is created when it is first shown, not with the window
            var header = new TextBlock { Text = title, HorizontalAlignment = HorizontalAlignment.Left };
            header.Classes.Add("Body1");
            return new TreeViewItem { Header = header, Tag = page };
        }

        // Closing from the system (Cmd+W, Alt+F4) behaves like the Close button: ask, then hide
        private void SettingsWindow_Closing(object sender, WindowClosingEventArgs e)
        {
            if (_forceClose)
            {
                return;
            }
            e.Cancel = true;
            CancelCommand.Execute(null);
        }

        private bool _forceClose;

        public void CloseForGood()
        {
            _forceClose = true;
            Close();
        }

        private void UpdateNotificationMessage()
        {
            RestartAppNotification.IsVisible = _mananger.RestartNeeded;
            NotificationMessage.Text = string.IsNullOrEmpty(_mananger.NewVersion)
                ? "Restart application to apply changes"
                : "Restart application to apply updates";
        }

        private void _mananger_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_mananger.RestartNeeded) || e.PropertyName == nameof(_mananger.NewVersion))
            {
                UpdateNotificationMessage();
            }
        }

        public void RefreshSelectedMenuItem()
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

            if (treeItem.Tag is Type pageType)
            {
                treeItem.Tag = _container.Resolve(pageType);
            }

            if (treeItem.Tag is IPluginSettingWindow pluginPage)
            {
                pluginPage.Refresh();
            }

            if (treeItem.Tag is Control page)
            {
                _settingPage = page;
                SettingGrid.Children.Add(_settingPage);
                Grid.SetColumn(_settingPage, 1);
                Grid.SetRow(_settingPage, 0);
                _settingPage.Margin = new Thickness(10, 20, 10, 10);
            }
        }

        private void Settings_OnMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            if (e.Handled || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                return;
            }
            try
            {
                BeginMoveDrag(e);
            }
            catch
            {
                // pointer already released
            }
        }
    }

    public class SaveSettingsCommand : ICommand
    {
        private readonly ISettings _settings;

        public SaveSettingsCommand(ISettings settings)
        {
            _settings = settings;
            _settings.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(_settings.HasUnsavedChanges))
                {
                    CanExecuteChanged?.Invoke(this, EventArgs.Empty);
                }
            };
        }

        public bool CanExecute(object parameter) => _settings.HasUnsavedChanges;

        public void Execute(object parameter) => _settings.Save();

        public event EventHandler CanExecuteChanged;
    }

    public class CancelSettingsCommand : ICommand
    {
        private readonly Window _settingsWindow;
        private readonly ISettings _settings;

        public CancelSettingsCommand(Window settingWindow, ISettings settings)
        {
            _settingsWindow = settingWindow;
            _settings = settings;
        }

        public bool CanExecute(object parameter) => true;

        public async void Execute(object parameter)
        {
            if (_settings.HasUnsavedChanges
                && await MessageBox.Show(_settingsWindow, "Do you want to cancel unsaved changes ?", "Cancel unsaved changes", MessageBoxButton.YesNo) == MessageBoxResult.No)
            {
                return;
            }

            if (_settings.HasUnsavedChanges)
            {
                _settings.Load();
            }
            _settingsWindow.Hide();
        }

        public event EventHandler CanExecuteChanged;
    }
}

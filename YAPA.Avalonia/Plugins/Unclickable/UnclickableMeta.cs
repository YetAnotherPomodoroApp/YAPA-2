using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Platform;
using YAPA.Shared.Contracts;

namespace YAPA.Avalonia.Plugins.Unclickable
{
    public class UnclickableMeta : IPluginMeta
    {
        public string Title => "Unclickable";

        public string Id => "Unclickable";

        public Type Plugin => typeof(UnclickablePlugin);

        public Type Settings => typeof(UnclickableSettings);

        public Type SettingEditWindow => typeof(UnclickableSettingsWindow);
    }

    /// <summary>
    /// Lets clicks fall through the timer while a work or break interval runs.
    /// Windows: WS_EX_TRANSPARENT, as the WPF plugin did. macOS: NSWindow ignoresMouseEvents.
    /// </summary>
    public class UnclickablePlugin : IPlugin
    {
        private readonly IPomodoroEngine _engine;
        private readonly UnclickableSettings _settings;
        private readonly Window _window;
        private bool _setOnce;
        private int _extendedStyle;
        private bool _styleSaved;

        public UnclickablePlugin(IApplication app, IPomodoroEngine engine, UnclickableSettings settings, ISettings globalSettings)
        {
            _engine = engine;
            _settings = settings;
            _window = (Window)app;
            _engine.PropertyChanged += _engine_PropertyChanged;
            globalSettings.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName.EndsWith(nameof(_settings.UnclickablityType)) && _settings.UnclickablityType != UnclickablityType.ClickThrough)
                {
                    SetClickThrough(false);
                }
            };
        }

        private void _engine_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (_settings.UnclickablityType != UnclickablityType.ClickThrough)
            {
                return;
            }

            if (_engine.Phase == PomodoroPhase.Break || _engine.Phase == PomodoroPhase.Work)
            {
                if (!_setOnce)
                {
                    SetClickThrough(true);
                    _setOnce = true;
                }
            }
            else if (_setOnce)
            {
                SetClickThrough(false);
                _setOnce = false;
            }
        }

        private void SetClickThrough(bool clickThrough)
        {
            var handle = _window.TryGetPlatformHandle();
            if (handle == null)
            {
                return;
            }

            if (OperatingSystem.IsWindows())
            {
                if (!_styleSaved)
                {
                    _extendedStyle = GetWindowLong(handle.Handle, GWL_EXSTYLE);
                    _styleSaved = true;
                }
                SetWindowLong(handle.Handle, GWL_EXSTYLE, clickThrough ? _extendedStyle | WS_EX_TRANSPARENT | WS_EX_LAYERED : _extendedStyle);
            }
            else if (OperatingSystem.IsMacOS() && handle is IMacOSTopLevelPlatformHandle mac && mac.NSWindow != IntPtr.Zero)
            {
                objc_msgSend(mac.NSWindow, sel_registerName("setIgnoresMouseEvents:"), clickThrough);
            }
        }

        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_LAYERED = 0x00080000;
        private const int GWL_EXSTYLE = -20;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        [DllImport("/usr/lib/libobjc.dylib")]
        private static extern IntPtr sel_registerName(string name);

        [DllImport("/usr/lib/libobjc.dylib")]
        private static extern void objc_msgSend(IntPtr receiver, IntPtr selector, bool value);
    }

    public enum UnclickablityType
    {
        ClickThrough
    }

    public class UnclickableSettings : IPluginSettings
    {
        private readonly ISettingsForComponent _settings;

        public UnclickablityType UnclickablityType
        {
            get => _settings.Get(nameof(UnclickablityType), UnclickablityType.ClickThrough);
            set => _settings.Update(nameof(UnclickablityType), value);
        }

        public UnclickableSettings(ISettings settings)
        {
            _settings = settings.GetSettingsForComponent(new UnclickableMeta().Id);
        }

        public void DeferChanges() => _settings.DeferChanges();
    }

    public class UnclickableSettingsWindow : UserControl
    {
        public UnclickableSettings Settings { get; }

        public UnclickableSettingsWindow(UnclickableSettings settings)
        {
            Settings = settings;
            Settings.DeferChanges();
            DataContext = this;

            // YAPA/Plugins/Unclickable/UnclickableSettingWindow.xaml
            var grid = new Grid
            {
                //The columns of the other settings pages
                ColumnDefinitions = new ColumnDefinitions("130,*"),
                RowDefinitions = new RowDefinitions("34"),
            };
            var label = new TextBlock { Text = "Behaviour", VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center };
            var list = new ComboBox
            {
                MinWidth = 200,
                HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Left,
                VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center,
                ItemsSource = new List<BehaviourListItem> { new BehaviourListItem { Item = UnclickablityType.ClickThrough, Title = "Click through" } },
                DisplayMemberBinding = new global::Avalonia.Data.Binding(nameof(BehaviourListItem.Title)),
                SelectedValueBinding = new global::Avalonia.Data.Binding(nameof(BehaviourListItem.Item)),
            };
            list.Bind(ComboBox.SelectedValueProperty, new global::Avalonia.Data.Binding("Settings.UnclickablityType") { Mode = global::Avalonia.Data.BindingMode.TwoWay });
            global::Avalonia.Automation.AutomationProperties.SetName(list, "Unclickable behaviour");
            Grid.SetColumn(list, 1);
            grid.Children.Add(label);
            grid.Children.Add(list);
            Content = grid;
        }

        public class BehaviourListItem
        {
            public UnclickablityType Item { get; set; }
            public string Title { get; set; }
        }
    }
}

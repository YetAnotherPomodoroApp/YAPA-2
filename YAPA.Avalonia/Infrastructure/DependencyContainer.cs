using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using YAPA.Avalonia.Settings;
using YAPA.Avalonia.Specifics;
using YAPA.Shared.Common;
using YAPA.Shared.Contracts;

namespace YAPA.Avalonia.Infrastructure
{
    /// <summary>The same registrations as YAPA/WPF/DependencyContainer.cs.</summary>
    public class DependencyContainer
    {
        public DependencyInjector Container { get; }
        public IPluginManager PluginManager { get; }
        public IThemeManager ThemeManager { get; }
        public Dashboard Dashboard { get; }
        public Window MainWindow { get; }

        /// <param name="overrides">Lets tests replace registrations (a fake clock) before anything is built.</param>
        public DependencyContainer(Action<DependencyInjector> overrides = null)
        {
            Container = ConfigureContainer();
            overrides?.Invoke(Container);

            //Themes
            ThemeManager = new ThemeManager(Container, GetTypes<IThemeMeta>(), Container.Resolve<ThemeManagerSettings>());
            Container.RegisterInstance(ThemeManager, typeof(IThemeManager));
            Container.Register(typeof(IApplication), ThemeManager.ActiveTheme.Theme, true);

            //Plugins
            PluginManager = new PluginManager(Container, GetTypes<IPluginMeta>(), Container.Resolve<PluginManagerSettings>(), Container.Resolve<ISettings>());
            Container.RegisterInstance(PluginManager, typeof(IPluginManager));

            PluginManager.InitPlugins();

            Dashboard = Container.Resolve<Dashboard>();

            MainWindow = (Window)Container.Resolve<IApplication>();
        }

        private static DependencyInjector ConfigureContainer()
        {
            var di = new DependencyInjector();

            di.Register<IEnvironment, AvaloniaEnvironment>(true);
            di.Register<IJson, Yapa.Shared.Common.NewftonsJson>(true);
            di.Register<ISettings, JsonYapaSettings>(true);
            di.Resolve<ISettings>(); // AutoActivate

            di.Register<IPomodoroEngine, PomodoroEngine>(true);
            di.Register(typeof(PomodoroEngineSettings));

            di.Register<ITimer, Timer>();
            di.Register<IMusicPlayer, SoundPlayer>();

            di.Register<IMainViewModel, MainViewModel>();
            di.Register<IThreading, AvaloniaThreading>();

            di.Register(typeof(ThemeManagerSettings));
            di.Register(typeof(PluginManagerSettings));
            di.Register(typeof(PluginManagerSettingWindow));
            di.Register(typeof(AboutPage));

            di.Register(typeof(Dashboard), true);

            di.Register<IPomodoroRepository, ItemRepository>(true);

            di.Register<IShowSettingsCommand, ShowSettingsCommand>();
            di.Register<IDate, DateTimeWrapper>();

            di.Register<ISettingManager, SettingManager>(true);

            // One settings window for the whole app, created the first time it is opened
            di.Register(typeof(SettingsWindow), true);

            di.Register<IFontService, FontService>();

            return di;
        }

        /// <summary>
        /// Themes and plugins are compiled into this app; the WPF version also loaded them from
        /// the Themes and Plugins folders next to YAPA.exe.
        /// </summary>
        private static IEnumerable<T> GetTypes<T>()
        {
            return typeof(DependencyContainer).Assembly.GetExportedTypes()
                .Where(t => !t.IsAbstract && t.GetInterfaces().Contains(typeof(T)) && t.GetConstructor(Type.EmptyTypes) != null)
                .OrderBy(t => t.FullName, StringComparer.Ordinal)
                .Select(t => (T)Activator.CreateInstance(t))
                .ToList();
        }
    }

    public class ShowSettingsCommand : IShowSettingsCommand
    {
        private readonly IDependencyInjector _container;

        public ShowSettingsCommand(IDependencyInjector container)
        {
            _container = container;
        }

        public bool CanExecute(object parameter) => true;

        public void Execute(object parameter)
        {
            var settings = (SettingsWindow)_container.Resolve(typeof(SettingsWindow));
            var owner = (Window)_container.Resolve(typeof(IApplication));
            if (settings.IsVisible)
            {
                settings.Activate();
                return;
            }

            settings.WindowStartupLocation = WindowStartupLocation.Manual;
            // Centre on the timer, as WindowStartupLocation.CenterOwner did, but stay on its screen
            var screen = owner.Screens.ScreenFromWindow(owner) ?? owner.Screens.Primary;
            if (screen != null)
            {
                var scaling = screen.Scaling;
                var width = (int)(settings.Width * scaling);
                var height = (int)(settings.Height * scaling);
                var ownerScaling = owner.ScreenScaling();
                var centerX = owner.Position.X + (int)(owner.Bounds.Width * ownerScaling / 2);
                var centerY = owner.Position.Y + (int)(owner.Bounds.Height * ownerScaling / 2);
                var area = screen.WorkingArea;
                var x = Math.Max(area.X, Math.Min(area.Right - width, centerX - width / 2));
                var y = Math.Max(area.Y, Math.Min(area.Bottom - height, centerY - height / 2));
                settings.Position = new global::Avalonia.PixelPoint(x, y);
            }

            if (owner.IsVisible)
            {
                settings.ShowDialog(owner);
            }
            else
            {
                settings.Show();
            }
        }

        public event EventHandler CanExecuteChanged;
    }
}

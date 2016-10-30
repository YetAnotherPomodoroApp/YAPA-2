using Autofac;
using Microsoft.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using YAPA.Contracts;
using YAPA.Shared;
using YAPA.Shared.Contracts;
using YAPA.WPF;
using YAPA.WPF.Plugins;

namespace YAPA
{
    public partial class App : Application, ISingleInstanceApp
    {
        private static IContainer Container { get; set; }
        private static IPluginManager PluginManager { get; set; }
        private static IThemeManager ThemeManager { get; set; }
        private static Dashboard Dashboard { get; set; }

        [STAThread]
        public static void Main()
        {
            if (SingleInstance<App>.InitializeAsFirstInstance("AdvancedJumpList"))
            {
                var application = new App();

                Container = ConfigureContainer();

                var di = new DependencyInjector(Container);
                di.RegisterInstance(di, typeof(IDependencyInjector));

                //Themes
                ThemeManager = new ThemeManager(di, GetThemeMetas(), (ThemeManagerSettings)Container.Resolve(typeof(ThemeManagerSettings)));
                var themeUpdater = new ContainerBuilder();
                themeUpdater.RegisterInstance(ThemeManager).As<IThemeManager>().SingleInstance();
                themeUpdater.RegisterType(ThemeManager.ActiveTheme.Theme).As<IApplication>().SingleInstance();
                themeUpdater.Update(Container);

                //Plugins
                PluginManager = new PluginManager(di, GetPluginMetas(), (PluginManagerSettings)Container.Resolve(typeof(PluginManagerSettings)));
                var updater = new ContainerBuilder();
                updater.RegisterInstance(PluginManager).As<IPluginManager>().SingleInstance();
                updater.Update(Container);

                PluginManager.InitPlugins();
                Dashboard = Container.Resolve<Dashboard>();

                //Load theme
                Current.MainWindow = (Window)Container.Resolve<IApplication>();

                Current.MainWindow.Show();
                Current.MainWindow.Closed += MainWindow_Closed;

                application.Init();
                application.Run();

                SingleInstance<App>.Cleanup();
            }
        }

        private static void MainWindow_Closed(object sender, EventArgs e)
        {
            Current.Shutdown();
        }

        private static IContainer ConfigureContainer()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<JsonYapaSettings>().As<ISettings>().AutoActivate().SingleInstance();
            builder.RegisterType<NewftonsJson>().As<IJson>().SingleInstance();

            builder.RegisterType(typeof(PomodoroEngine)).As<IPomodoroEngine>().SingleInstance();
            builder.RegisterType(typeof(PomodoroEngineSettings)).As<PomodoroEngineSettings>();
            builder.RegisterType(typeof(PomodoroEngineSettingWindow)).As<PomodoroEngineSettingWindow>();

            builder.RegisterType(typeof(Timer)).As<ITimer>();
            builder.RegisterType(typeof(MusicPlayer)).As<IMusicPlayer>();

            builder.RegisterType(typeof(MainViewModel)).As<IMainViewModel>();

            builder.RegisterType(typeof(ThemeManagerSettings));
            builder.RegisterType(typeof(ThemeManagerSettingWindow));

            builder.RegisterType(typeof(PluginManagerSettings));
            builder.RegisterType(typeof(PluginManagerSettingWindow));

            builder.RegisterType(typeof(GithubDashboard));
            builder.RegisterType(typeof(Dashboard)).SingleInstance();

            builder.RegisterType(typeof(WpfEnviroment)).As<IEnviroment>().SingleInstance();

            builder.RegisterType(typeof(ItemRepository)).As<IPomodoroRepository>().SingleInstance();

            builder.RegisterType(typeof(ShowSettingsCommand)).As<IShowSettingsCommand>();


            builder.RegisterType(typeof(SettingManager)).As<ISettingManager>().SingleInstance();

            var container = builder.Build();

            var updater = new ContainerBuilder();
            updater.RegisterInstance(container).As<IContainer>();
            updater.Update(container);

            return container;
        }

        private static IEnumerable<IPluginMeta> GetPluginMetas()
        {
            foreach (IPluginMeta meta in from plugin in Assembly.GetExecutingAssembly().GetTypes()
                                         where plugin.GetInterfaces().Contains(typeof(IPluginMeta))
                                         select (IPluginMeta)Activator.CreateInstance(plugin))
            {
                yield return meta;
            }

            foreach (IPluginMeta meta in from plugin in Assembly.LoadFrom("YAPA.WPF.Shared.dll").GetTypes()
                                         where plugin.GetInterfaces().Contains(typeof(IPluginMeta))
                                         select (IPluginMeta)Activator.CreateInstance(plugin))
            {
                yield return meta;
            }
        }

        private static IEnumerable<IThemeMeta> GetThemeMetas()
        {
            foreach (IThemeMeta meta in from plugin in Assembly.GetExecutingAssembly().GetTypes()
                                        where plugin.GetInterfaces().Contains(typeof(IThemeMeta))
                                        select (IThemeMeta)Activator.CreateInstance(plugin))
            {
                yield return meta;
            }

            foreach (IThemeMeta meta in from plugin in Assembly.LoadFrom("YAPA.WPF.Shared.dll").GetTypes()
                                        where plugin.GetInterfaces().Contains(typeof(IThemeMeta))
                                        select (IThemeMeta)Activator.CreateInstance(plugin))
            {
                yield return meta;
            }

            foreach (IThemeMeta meta in from plugin in Assembly.LoadFrom("YAPA.WPF.Themes.dll").GetTypes()
                                        where plugin.GetInterfaces().Contains(typeof(IThemeMeta))
                                        select (IThemeMeta)Activator.CreateInstance(plugin))
            {
                yield return meta;
            }
        }

        public void Init()
        {
            InitializeComponent();
        }

        #region ISingleInstanceApp Members

        public bool SignalExternalCommandLineArgs(IList<string> args)
        {
            if (args == null || args.Count == 0)
            {
                return true;
            }
            return true;//MainWindow.ProcessCommandLineArgs(args.ToArray());
        }

        #endregion
    }
}

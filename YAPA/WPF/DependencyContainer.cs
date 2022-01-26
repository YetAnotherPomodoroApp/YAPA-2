using Autofac;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using Microsoft.Extensions.Logging;
using NLog;
using YAPA.Plugins.Dashboard;
using YAPA.Plugins.PomodoroEngine;
using YAPA.Shared.Common;
using YAPA.Shared.Contracts;
using YAPA.WPF.PluginManager;
using YAPA.WPF.SettingsMananger;
using YAPA.WPF.Specifics;
using YAPA.WPF.ThemeManager;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using YAPA.WPF.Shared.Common;

namespace YAPA.WPF
{
    public class DependencyContainer
    {
        public IContainer Container { get; }
        public IPluginManager PluginManager { get; }
        public IThemeManager ThemeManager { get; }
        public Dashboard Dashboard { get; }
        public Window MainWindow { get; }

        public static Dictionary<string, bool> LoadedAssemblies { get; } = new Dictionary<string, bool>();

        public DependencyContainer()
        {
            Container = ConfigureContainer();

            var di = new DependencyInjector(Container);
            di.RegisterInstance(di, typeof(IDependencyInjector));

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            //Themes
            ThemeManager = new ThemeManager.ThemeManager(di, GetThemeMetas(), (ThemeManagerSettings)Container.Resolve(typeof(ThemeManagerSettings)));
            var themeUpdater = new ContainerBuilder();
            themeUpdater.RegisterInstance(ThemeManager).As<IThemeManager>().SingleInstance();
            themeUpdater.RegisterType(ThemeManager.ActiveTheme.Theme).As<IApplication>().SingleInstance();
            themeUpdater.Update(Container);

            //Plugins
            PluginManager = new PluginManager.PluginManager(di, GetPluginMetas(), Container.Resolve<PluginManagerSettings>(), Container.Resolve<ISettings>());
            var updater = new ContainerBuilder();
            updater.RegisterInstance(PluginManager).As<IPluginManager>().SingleInstance();
            updater.Update(Container);

            PluginManager.InitPlugins();

            Dashboard = Container.Resolve<Dashboard>();

            MainWindow = (Window)Container.Resolve<IApplication>();
        }

        private static IContainer ConfigureContainer()
        {
            var builder = new ContainerBuilder();

            builder.RegisterInstance(LogManager.GetLogger("YAPA2")).As<NLog.ILogger>();

            builder.RegisterType<JsonYapaSettings>().As<ISettings>().AutoActivate().SingleInstance();

            builder.RegisterType<NewftonsJson>().As<IJson>().SingleInstance();

            builder.RegisterType(typeof(PomodoroEngine)).As<IPomodoroEngine>().SingleInstance();
            builder.RegisterType(typeof(PomodoroEngineSettings)).As<PomodoroEngineSettings>();

            builder.RegisterType(typeof(Timer)).As<ITimer>();
            builder.RegisterType(typeof(SoundPlayer)).As<IMusicPlayer>();

            builder.RegisterType(typeof(MainViewModel)).As<IMainViewModel>();

            builder.RegisterType(typeof(WpfThreading)).As<IThreading>();

            builder.RegisterType(typeof(ThemeManagerSettings));
            //builder.RegisterType(typeof(ThemeManagerSettingWindow));

            builder.RegisterType(typeof(PluginManagerSettings));
            builder.RegisterType(typeof(PluginManagerSettingWindow));

            builder.RegisterType(typeof(AboutPage));

            builder.RegisterType(typeof(GithubDashboard));
            builder.RegisterType(typeof(Dashboard)).SingleInstance();

            builder.RegisterType(typeof(WpfEnviroment)).As<IEnvironment>().SingleInstance();

            builder.RegisterType(typeof(ItemRepository)).As<IPomodoroRepository>().SingleInstance();

            builder.RegisterType(typeof(ShowSettingsCommand)).As<IShowSettingsCommand>();
            builder.RegisterType(typeof(DateTimeWrapper)).As<IDate>();

            builder.RegisterType(typeof(SettingManager)).As<ISettingManager>().SingleInstance();

            builder.RegisterType(typeof(PomodoroProfileSettings));

            builder.RegisterType(typeof(SettingsWindow)).As<SettingsWindow>().SingleInstance();

            builder.RegisterType(typeof(FontService)).As<IFontService>();


            var container = builder.Build();

            var updater = new ContainerBuilder();
            updater.RegisterInstance(container).As<IContainer>();
            updater.Update(container);

            return container;
        }

        private static IEnumerable<IPluginMeta> GetPluginMetas()
        {
            return GetTypes<IPluginMeta>("Plugins");
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var location = args?.RequestingAssembly?.Location;
            if (string.IsNullOrEmpty(location))
            {
                return null;
            }
            return Assembly.LoadFile(location);
        }

        private static IEnumerable<IThemeMeta> GetThemeMetas()
        {
            return GetTypes<IThemeMeta>("Themes");
        }

        private static IEnumerable<T> GetTypes<T>(params string[] folders)
        {
            var exePath = AppDomain.CurrentDomain.BaseDirectory;

            var assembliesInDirectory = folders
                .Select(x => Path.Combine(exePath, x))
                    .Where(Directory.Exists)
                    .SelectMany(x => Directory.GetFiles(x, "*.dll", SearchOption.AllDirectories))
                    .Distinct();

            var results = new List<T>();

            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
            var loadedAssemblyPaths = loadedAssemblies.Where(x => !x.IsDynamic).Select(a => Path.GetFileName(a.Location)).ToArray();

            var assembliesToLoad = assembliesInDirectory.Where(r => !loadedAssemblyPaths.Contains(Path.GetFileName(r), StringComparer.InvariantCultureIgnoreCase)).ToList();
            assembliesToLoad.ForEach(path => loadedAssemblies.Add(AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(path))));

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(x => !x.IsDynamic))
            {
                foreach (var t in assembly.GetExportedTypes())
                {
                    if (t.GetInterfaces().Contains(typeof(T)))
                    {
                        results.Add((T)Activator.CreateInstance(t));
                    }
                }
            }

            return results;
        }

    }
}

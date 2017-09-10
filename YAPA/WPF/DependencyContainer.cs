using Autofac;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using YAPA.Plugins.Dashboard;
using YAPA.Plugins.PomodoroEngine;
using YAPA.Shared.Common;
using YAPA.Shared.Contracts;
using YAPA.WPF.PluginManager;
using YAPA.WPF.SettingsMananger;
using YAPA.WPF.Specifics;
using YAPA.WPF.ThemeManager;

namespace YAPA.WPF
{
    public class DependencyContainer
    {
        public IContainer Container { get; private set; }
        public IPluginManager PluginManager { get; private set; }
        public IThemeManager ThemeManager { get; private set; }
        public Dashboard Dashboard { get; private set; }
        public Window MainWindow { get; private set; }

        private static Dictionary<string, bool> LoadedAssemblies = new Dictionary<string, bool>();

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
            builder.RegisterType(typeof(PluginManager.PluginManagerSettingWindow));

            builder.RegisterType(typeof(AboutPage));

            builder.RegisterType(typeof(GithubDashboard));
            builder.RegisterType(typeof(Dashboard)).SingleInstance();

            builder.RegisterType(typeof(WpfEnviroment)).As<IEnvironment>().SingleInstance();

            builder.RegisterType(typeof(ItemRepository)).As<IPomodoroRepository>().SingleInstance();

            builder.RegisterType(typeof(ShowSettingsCommand)).As<IShowSettingsCommand>();
            builder.RegisterType(typeof(DateTimeWrapper)).As<IDate>();

            builder.RegisterType(typeof(SettingManager)).As<ISettingManager>().SingleInstance();

            builder.RegisterType(typeof(PomodoroProfileSettings));


            var container = builder.Build();

            var updater = new ContainerBuilder();
            updater.RegisterInstance(container).As<IContainer>();
            updater.Update(container);

            return container;
        }

        private static IEnumerable<IPluginMeta> GetPluginMetas()
        {
            var metas = new List<IPluginMeta>();

            foreach (IPluginMeta meta in from plugin in Assembly.GetExecutingAssembly().GetTypes()
                                         where plugin.GetInterfaces().Contains(typeof(IPluginMeta))
                                         select (IPluginMeta)Activator.CreateInstance(plugin))
            {
                metas.Add(meta);
            }

            foreach (var externalPlugin in GetTypes<IPluginMeta>("Plugins"))
            {
                metas.Add(externalPlugin);
            }
            return metas;
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            return Assembly.LoadFile(args.RequestingAssembly.Location);
        }

        private static IEnumerable<IThemeMeta> GetThemeMetas()
        {
            return GetTypes<IThemeMeta>("Themes");
        }

        private static IEnumerable<T> GetTypes<T>(params string[] folders)
        {
            var themeFiles = folders
                    .Where(Directory.Exists)
                    .SelectMany(x => Directory.GetFiles(x, "*.dll", SearchOption.AllDirectories))
                    .Distinct();

            var exePath = AppDomain.CurrentDomain.BaseDirectory;

            var results = new List<T>();

            foreach (var file in themeFiles)
            {
                Type[] types = Array.Empty<Type>();
                try
                {
                    var assemblyPath = Path.Combine(exePath, file);
                    if (LoadedAssemblies.ContainsKey(assemblyPath))
                    {
                        continue;
                    }
                    types = Assembly.LoadFile(assemblyPath).GetTypes();
                    LoadedAssemblies[assemblyPath] = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                if (types.Any())
                {
                    foreach (var type in types.Where(x => x.GetInterfaces().Contains(typeof(T))))
                    {
                        results.Add((T)Activator.CreateInstance(type));
                    }
                }
            }
            return results;
        }

    }
}

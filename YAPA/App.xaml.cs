using Autofac;
using Microsoft.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using YAPA.Contracts;
using YAPA.Shared;
using YAPA.WPF;

namespace YAPA
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application, ISingleInstanceApp
    {
        private static IContainer Container { get; set; }
        private static IEnumerable<IPlugin> Plugins;

        [STAThread]
        public static void Main()
        {
            if (SingleInstance<App>.InitializeAsFirstInstance("AdvancedJumpList"))
            {
                var application = new App();

                Container = ConfigureContainer();

                Current.MainWindow = (Window)Container.Resolve<IApplication>();
                Plugins = StartPlugins(Container);

                Current.MainWindow.Show();

                application.Init();
                application.Run();

                //Load plugins

                SingleInstance<App>.Cleanup();
            }
        }



        private static IContainer ConfigureContainer()
        {
            var builder = new ContainerBuilder();
            builder.RegisterInstance(new JsonYapaSettings()).As<ISettings>().SingleInstance();

            builder.RegisterType(typeof(MainWindow)).As<IApplication>().SingleInstance();
            builder.RegisterType(typeof(PomodoroEngine)).As<IPomodoroEngine>().SingleInstance();

            builder.RegisterType(typeof(Timer)).As<ITimer>();
            builder.RegisterType(typeof(MusicPlayer)).As<IMusicPlayer>();

            RegisterPluginSettings(builder);
            RegisterPlugins(builder);
            return builder.Build();
        }

        private static void RegisterPluginSettings(ContainerBuilder builder)
        {
            var plugins = GetType(typeof(IPluginSettings));

            foreach (var plugin in plugins)
            {
                builder.RegisterType(plugin).SingleInstance();
            }
        }

        private static void RegisterPlugins(ContainerBuilder builder)
        {
            var plugins = GetType(typeof(IPlugin));

            foreach (var plugin in plugins)
            {
                builder.RegisterType(plugin).SingleInstance();
            }
        }

        private static IEnumerable<IPlugin> StartPlugins(IContainer container)
        {
            var plugins = GetType(typeof(IPlugin));

            return plugins.Select(plugin => (IPlugin)container.Resolve(plugin)).ToList();
        }

        private static IEnumerable<Type> GetType(Type t)
        {
            return from plugin in Assembly.GetExecutingAssembly().GetTypes()
                   where plugin.GetInterfaces().Contains(t)
                   select plugin;
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
            return ((MainWindow)MainWindow).ProcessCommandLineArgs(args.ToArray());
        }

        #endregion
    }
}

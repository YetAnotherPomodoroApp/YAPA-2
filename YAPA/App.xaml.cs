using Autofac;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using NLog;
using Squirrel;
using YAPA.Shared.Common;
using YAPA.Shared.Contracts;
using YAPA.WPF;

namespace YAPA
{
    public partial class App : ISingleInstanceApp
    {
        private static IContainer Container { get; set; }
        private static IPluginManager PluginManager { get; set; }
        private static IThemeManager ThemeManager { get; set; }
        private static Dashboard Dashboard { get; set; }

        [STAThread]
        public static void Main()
        {
            if (SingleInstance<App>.InitializeAsFirstInstance("YAPA2"))
            {
                var application = new App();

                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException; ;

                var dCont = new DependencyContainer();
                Container = dCont.Container;
                PluginManager = dCont.PluginManager;
                ThemeManager = dCont.ThemeManager;
                Dashboard = dCont.Dashboard;

                //Load theme
                Current.MainWindow = (Window)dCont.MainWindow;
#if !DEBUG
                Task.Run(async () =>
                {
                    await Update(Container.Resolve<ISettingManager>(),Container.Resolve<IEnvironment>());
                });
#endif

                Current.MainWindow.Show();
                Current.MainWindow.Closed += MainWindow_Closed;

                application.Init();
                application.Run();


                SingleInstance<App>.Cleanup();
            }
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var logger = LogManager.GetLogger("YAPA2");
            logger.Fatal($"Unhandled exception: {e.ExceptionObject}");
        }

        private static void MainWindow_Closed(object sender, EventArgs e)
        {
            Current.Shutdown();
        }

        private static async Task Update(ISettingManager settings, IEnvironment environment)
        {
            try
            {
                var releaseUrl = @"ftp://s1.floatas.net/yapa-2/";
                var preReleaseUrl = @"ftp://s1.floatas.net/yapa-2-pre-release/";

                var updateUrl = environment.PreRelease() ? preReleaseUrl : releaseUrl;

                using (var mgr = new UpdateManager(updateUrl))
                {
                    var update = await mgr.UpdateApp();
                    if (!string.IsNullOrEmpty(update?.Filename))
                    {
                        settings.RestartNeeded = true;
                        settings.NewVersion = update.Version.ToString();
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        public void Init()
        {
            InitializeComponent();
        }

        #region ISingleInstanceApp Members

        public bool SignalExternalCommandLineArgs(IList<string> args)
        {
            //the first index always contains the location of the exe
            if (args == null || args.Count < 2 || Current.MainWindow == null)
            {
                return true;
            }
            var arg = args[1];
            return ((IApplication)Current.MainWindow).ProcessCommandLineArg(arg);
        }

        #endregion
    }
}

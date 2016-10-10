using Autofac;
using Microsoft.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using YAPA.Contracts;
using YAPA.WPF;

namespace YAPA
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application, ISingleInstanceApp
    {
        private static IContainer Container { get; set; }

        [STAThread]
        public static void Main()
        {
            if (SingleInstance<App>.InitializeAsFirstInstance("AdvancedJumpList"))
            {
                var application = new App();

                Container = ConfigureContainer();

                Current.MainWindow = (Window)Container.Resolve<IApplication>();
                Current.MainWindow.Show();

                application.Init();
                application.Run();

                SingleInstance<App>.Cleanup();
            }
        }

        private static IContainer ConfigureContainer()
        {
            var builder = new ContainerBuilder();
            builder.RegisterInstance(new JsonYapaSettings()).As<ISettings>();

            builder.RegisterType(typeof(MainWindow)).As<IApplication>();

            return builder.Build();
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

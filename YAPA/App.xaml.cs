using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Microsoft.Shell;

namespace YAPA
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application, ISingleInstanceApp
    {
        [STAThread]
        public static void Main()
        {
            if (SingleInstance<App>.InitializeAsFirstInstance("AdvancedJumpList"))
            {
                var application = new App();

                application.Init();
                application.Run();

                // Allow single instance code to perform cleanup operations
                SingleInstance<App>.Cleanup();
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
            return ((MainWindow)MainWindow).ProcessCommandLineArgs(args.ToArray());
        }

        #endregion
    }
}

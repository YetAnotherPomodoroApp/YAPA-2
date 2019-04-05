using System;
using System.Collections.Generic;
using System.Windows;

namespace Motivational
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        [STAThread]
        public static void Main()
        {
            var application = new App();

            application.Init();
            application.Run();

            // Allow single instance code to perform cleanup operations
        }

        public void Init()
        {
            this.InitializeComponent();
        }

        public bool SignalExternalCommandLineArgs(IList<string> args)
        {
            return true;
        }
    }
}

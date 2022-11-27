using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using YAPA.Plugins.SaveApplicationPossitionOnScreen;
using YAPA.Shared.Contracts;

namespace YAPA.Plugins.ReleaseNotes
{
    /// <summary>
    /// Interaction logic for ReleaseNotesWindow.xaml
    /// </summary>
    public partial class ReleaseNotesWindow : Window
    {
        public ReleaseNotesSettings ReleaseNotesSettings { get; }
        private string releaseNotes = "https://github.com/YetAnotherPomodoroApp/YAPA-2/releases/latest";
        private string preReleaseNotes = "https://github.com/YetAnotherPomodoroApp/YAPA-2/compare/pre-release";
        private readonly SaveApplicationPositionOnScreenSettings _positionSettings;
        private readonly bool _preRelease;

        public ReleaseNotesWindow(ReleaseNotesSettings releaseNotesSettings, SaveApplicationPossitionOnScreen.SaveApplicationPositionOnScreenSettings _positionSettings, bool preRelease)
        {
            InitializeComponent();

            ReleaseNotesSettings = releaseNotesSettings;
            this._positionSettings = _positionSettings;
            _preRelease = preRelease;

            DataContext = this;

            Loaded += ReleaseNotesWindow_Loaded;
        }

        private void ReleaseNotesWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var originalHeigh = Height;
                var originalWidth = Width;
                WindowPlacement.SetPlacement(new WindowInteropHelper(this).Handle, _positionSettings.Position);
                Height = originalHeigh;
                Width = originalWidth;
            }
            catch
            {
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            // for .NET Core you need to add UseShellExecute = true
            // see https://learn.microsoft.com/dotnet/api/system.diagnostics.processstartinfo.useshellexecute#property-value
            Process.Start(new ProcessStartInfo(_preRelease ? preReleaseNotes : releaseNotes));
            e.Handled = true;
        }
    }
}

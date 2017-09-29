using System.Windows;

namespace YAPA.WPF
{
    public partial class ReleaseNotesWindow
    {
        private readonly string _releaseNotes;

        public ReleaseNotesWindow(string releaseNotes)
        {
            _releaseNotes = releaseNotes;
            InitializeComponent();
            Loaded += ReleaseNotesWindow_Loaded;
        }

        private void ReleaseNotesWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Browser.NavigateToString(_releaseNotes);
        }

        private void CloseButon_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

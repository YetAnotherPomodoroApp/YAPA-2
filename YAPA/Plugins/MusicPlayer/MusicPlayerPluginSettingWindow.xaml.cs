using System;
using System.Windows.Controls;
using System.Windows.Input;
using YAPA.Shared;

namespace YAPA.Plugins
{
    /// <summary>
    /// Interaction logic for MusicPlayerSettingWindow.xaml
    /// </summary>
    public partial class MusicPlayerPluginSettingWindow : UserControl
    {
        public MusicPlayerPluginSettingWindow(MusicPlayerPluginSettings settings)
        {
            settings.DeferChanges();

            InitializeComponent();

            DataContext = settings;

            BrowseBreakSong.Command = new BrowseSong(BreakSong);
            BrowseWorkSong.Command = new BrowseSong(WorkSong);
        }


        public class BrowseSong : ICommand
        {
            private readonly TextBox _output;

            public BrowseSong(TextBox output)
            {
                _output = output;
            }

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public void Execute(object parameter)
            {
                var dlg = new Microsoft.Win32.OpenFileDialog
                {
                    DefaultExt = ".wav",
                    Filter = "MP3 (*.mp3)|*.mp3|WAVE (*.wav)|*.wav|All Files(*.*)|*.*",
                    InitialDirectory = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources\\sounds")
                };

                var result = dlg.ShowDialog();


                if (result.HasValue && result == true)
                {
                    _output.Text = dlg.FileName;
                }

            }

            public event EventHandler CanExecuteChanged;
        }
    }
}

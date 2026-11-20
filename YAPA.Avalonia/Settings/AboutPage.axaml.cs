using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using YAPA.Avalonia.Infrastructure;
using YAPA.Shared.Contracts;

namespace YAPA.Avalonia.Settings
{
    public partial class AboutPage : UserControl
    {
        private readonly IPomodoroRepository _pomodoroRepository;

        public AboutPage()
        {
            InitializeComponent();
        }

        public AboutPage(IPomodoroRepository pomodoroRepository)
        {
            _pomodoroRepository = pomodoroRepository;
            InitializeComponent();
        }

        private void Homepage_Click(object sender, RoutedEventArgs e)
        {
            AbstractWindow.OpenUrl("https://github.com/YetAnotherPomodoroApp/YAPA-2");
        }

        private void Mail_Click(object sender, RoutedEventArgs e)
        {
            AbstractWindow.OpenUrl("mailto:pomodoroapp@outlook.com?subject=YAPA 2");
        }

        private async void Import_OnClick(object sender, RoutedEventArgs e)
        {
            var window = TopLevel.GetTopLevel(this) as Window;
            if (window == null)
            {
                return;
            }

            var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = false,
                FileTypeFilter = new[] { new FilePickerFileType("CSV (*.csv)") { Patterns = new[] { "*.csv" } } }
            });
            var path = files.FirstOrDefault()?.TryGetLocalPath();
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            //We expect csv
            //Date,int
            try
            {
                var pomodoros = new List<PomodoroEntity>();
                foreach (var line in File.ReadLines(path))
                {
                    var parts = line.Split(',');
                    if (parts.Length != 2)
                    {
                        await MessageBox.Show(window, $"Invalid line in file:{line}", "Error while importing");
                        return;
                    }

                    var date = DateTime.Parse(parts[0], CultureInfo.CurrentCulture).Date;
                    var completed = int.Parse(parts[1], CultureInfo.InvariantCulture);
                    for (var i = 0; i < completed; i++)
                    {
                        pomodoros.Add(new PomodoroEntity { DateTime = date, Count = 1 });
                    }
                }

                foreach (var pomodoro in pomodoros)
                {
                    _pomodoroRepository.Add(pomodoro);
                }

                await MessageBox.Show(window, "Imported successfully", "Import");
            }
            catch (Exception ex)
            {
                await MessageBox.Show(window, ex.Message, "Error while importing");
            }
        }
    }
}

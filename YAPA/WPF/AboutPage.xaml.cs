using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using YAPA.Shared.Contracts;

namespace YAPA.WPF
{
    public partial class AboutPage : UserControl
    {
        private readonly IPomodoroRepository _pomodoroRepository;

        public AboutPage(IPomodoroRepository pomodoroRepository)
        {
            _pomodoroRepository = pomodoroRepository;
            InitializeComponent();
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(((Hyperlink)sender).NavigateUri.ToString());
        }


        private void Import_OnClick(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".csv",
                Filter = "CSV (*.csv)|*.csv"
            };

            var result = dlg.ShowDialog();


            if (result == null || result == false)
            {
                return;
            }

            //We expect csv
            //Date,int
            try
            {
                var pomodoros = new List<PomodoroEntity>();

                using (var input = new StreamReader(dlg.FileName))
                {
                    string line;
                    while ((line = input.ReadLine()) != null)
                    {
                        var parts = line.Split(',');
                        if (parts.Length != 2)
                        {
                            MessageBox.Show($"Invalid line in file:{line}", "Error while importing", MessageBoxButton.OK);

                            return;
                        }
                        var date = DateTime.Parse(parts[0]).Date;
                        var completed = Int32.Parse(parts[1]);

                        for (int i = 0; i < completed; i++)
                        {
                            pomodoros.Add(new PomodoroEntity { DateTime = date, Count = 1 });
                        }
                    }
                }

                foreach (var pomodoro in pomodoros)
                {
                    _pomodoroRepository.Add(pomodoro);
                }

                MessageBox.Show("Imported successfully", "Import", MessageBoxButton.OK);
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error while importing", MessageBoxButton.OK);
            }
        }
    }
}

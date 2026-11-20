using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using YAPA.Avalonia.Infrastructure;
using YAPA.Avalonia.Settings;
using YAPA.Avalonia.Specifics;
using YAPA.Shared.Common;
using YAPA.Shared.Contracts;

namespace YAPA.Avalonia
{
    public class App : Application
    {
        private static DependencyContainer Container { get; set; }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;

                Container = new DependencyContainer();
                var mainWindow = Container.MainWindow;
                desktop.MainWindow = mainWindow;

                mainWindow.Opened += MainWindow_Loaded;
                mainWindow.Closing += MainWindowOnClosing;

                Program.Instance.ArgumentsReceived += args =>
                {
                    // the first argument is the program itself when it comes from a shortcut
                    var arg = args.LastOrDefault(x => x.StartsWith("/"));
                    if (arg != null)
                    {
                        Dispatcher.UIThread.Post(() => ((IApplication)mainWindow).ProcessCommandLineArg(arg));
                    }
                };
            }

            base.OnFrameworkInitializationCompleted();
        }

        private static void MainWindowOnClosing(object sender, WindowClosingEventArgs e)
        {
            if (e.Cancel)
            {
                return;
            }
            // The "timer is running" question cancels the first close and closes again
            Dispatcher.UIThread.Post(() =>
            {
                if (((Window)sender).IsVisible)
                {
                    return;
                }
                try
                {
                    ((SettingsWindow)Container.Container.Resolve(typeof(SettingsWindow))).CloseForGood();
                }
                catch
                {
                    // never opened
                }
            });
            SaveSnapshot();
        }

        private static string SnapshotFile() => Path.Combine(AvaloniaEnvironment.BaseDir, "snapshot.json");

        private static void SaveSnapshot()
        {
            try
            {
                var json = Container.Container.Resolve<IJson>();
                var engine = Container.Container.Resolve<IPomodoroEngine>();
                File.WriteAllText(SnapshotFile(), json.Serialize(engine.GetSnapshot()));
            }
            catch
            {
                // nothing to resume next time
            }
        }

        private static void MainWindow_Loaded(object sender, EventArgs e)
        {
            ((Window)sender).Opened -= MainWindow_Loaded;
            LoadSnapshot((Window)sender);
        }

        private static async void LoadSnapshot(Window owner)
        {
            var engine = Container.Container.Resolve<IPomodoroEngine>();
            var date = Container.Container.Resolve<IDate>();
            var json = Container.Container.Resolve<IJson>();

            try
            {
                var file = SnapshotFile();
                if (!File.Exists(file))
                {
                    return;
                }

                var snapshotJson = File.ReadAllText(file);
                RemoveSnapshotFile();
                if (string.IsNullOrEmpty(snapshotJson))
                {
                    return;
                }

                var snapshot = json.Deserialize<PomodoroEngineSnapshot>(snapshotJson);
                if (snapshot == null)
                {
                    return;
                }

                var args = Environment.GetCommandLineArgs();
                var startImmediately = args.Select(x => x.ToLowerInvariant()).Contains(CommandLineArguments.Start);

                var remainingTime = TimeSpan.FromSeconds(snapshot.PomodoroProfile.WorkTime - snapshot.PausedTime);
                if (snapshot.Phase == PomodoroPhase.Work || snapshot.Phase == PomodoroPhase.Pause)
                {
                    if (startImmediately || await MessageBox.Show(owner,
                            $"Remaining time: {remainingTime.Minutes:00}:{remainingTime.Seconds:00}. Resume pomodoro ?",
                            "Unfinished pomodoro", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        snapshot.StartDate = date.DateTimeUtc();
                        engine.LoadSnapshot(snapshot);
                    }
                }
            }
            catch
            {
                //Ignore corrupted snapshots
            }
        }

        private static void RemoveSnapshotFile()
        {
            try
            {
                File.Delete(SnapshotFile());
            }
            catch
            {
                //Ignore
            }
        }
    }
}

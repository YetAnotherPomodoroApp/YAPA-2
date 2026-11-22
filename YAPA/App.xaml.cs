using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Autofac;
using NLog;
using SimpleFeedReader;
using Squirrel;
using YAPA.Shared;
using YAPA.Shared.Common;
using YAPA.Shared.Contracts;
using YAPA.WPF;
using YAPA.WPF.SettingsMananger;
using YAPA.WPF.Specifics;
using IContainer = Autofac.IContainer;

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
            //Before anything else of the application is loaded, so that a start that fails can be recovered from
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            Start();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Start()
        {
            if (SingleInstance<App>.InitializeAsFirstInstance("YAPA2"))
            {
                //Only in the copy that runs: a command launch (/start, /stop) just hands over its argument,
                //and starting crash reporting there doubled its time. A crash before this point (even one in
                //compiling Start) starts crash reporting from the crash handler to send that report.
                var crashReporting = StartCrashReporting();

                var application = new App();

                application.DispatcherUnhandledException += TaskbarFailureGuard.Handle;

                var dCont = new DependencyContainer();
                Container = dCont.Container;
                FollowCrashReportingSetting();
                PluginManager = dCont.PluginManager;
                ThemeManager = dCont.ThemeManager;
                Dashboard = dCont.Dashboard;

                //Load theme
                Current.MainWindow = (Window)dCont.MainWindow;
#if !DEBUG
                Task.Run(async () =>
                {
                    await Update(Container.Resolve<ISettingManager>(), Container.Resolve<IEnvironment>(), Container.Resolve<PomodoroEngineSettings>());
                });
#endif

                Current.MainWindow.Loaded += MainWindow_Loaded;
                //The window is up and everything queued for its start has run: from here on a crash is not a failed start
                Current.MainWindow.Loaded += (sender, args) =>
                    Current.Dispatcher.BeginInvoke(new Action(StartupRecovery.MarkStartupCompleted), DispatcherPriority.ApplicationIdle);
                Current.MainWindow.Closing += MainWindowOnClosing;

                //Before Show: the window's start can open dialogs (release notes, a crash report to send)
                //that wait for the user, and the start is logged whether or not they are answered
                var logger = LogManager.GetLogger("YAPA2");
                logger.Info("Application starting");

                Current.MainWindow.Show();
                Current.MainWindow.Closed += MainWindow_Closed;


#if RELEASE_SAFETY_TEST
                //Test builds only (Build\release-safety)
                if (Environment.GetCommandLineArgs().Any(x => x.Equals(CrashTestArgument, StringComparison.OrdinalIgnoreCase)))
                {
                    //Checks crash reporting end to end: a crash on the UI thread two seconds after the start
                    //has completed, the way a crash while the timer is in use happens
                    var crashTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
                    crashTimer.Tick += (s, e) =>
                    {
                        crashTimer.Stop();
                        throw new InvalidOperationException("Crash test: YAPA was started with /crashtest");
                    };
                    //Once the application is idle, i.e. after the window and everything for its start has run
                    Current.Dispatcher.BeginInvoke(new Action(crashTimer.Start), DispatcherPriority.ApplicationIdle);
                }
#endif

                application.Init();
                application.Run();

                SingleInstance<App>.Cleanup();
                StopCrashReporting(crashReporting);
            }
        }

#if RELEASE_SAFETY_TEST
        private const string CrashTestArgument = "/crashtest";
#endif

        private static void MainWindowOnClosing(object sender, CancelEventArgs cancelEventArgs)
        {
            try
            {
                var settingsWindow = Container.Resolve<SettingsWindow>();
                settingsWindow.Close();
            }
            catch
            {
            }

            SaveSnapshot();
        }

        private static void SaveSnapshot()
        {
            string baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"YAPA2");
            var json = Container.Resolve<IJson>();
            var engine = Container.Resolve<IPomodoroEngine>();

            var file = Path.Combine(baseDir, "snapshot.json");
            File.WriteAllText(file, json.Serialize(engine.GetSnapshot()));
        }

        private static void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSnapshot();

            //After the window is up: a crash report from an earlier run, shown before it is sent
            var window = (Window)sender;
            Current.Dispatcher.BeginInvoke(new Action(() => AskAboutCrashReports(window)), DispatcherPriority.ApplicationIdle);

            var settings = Container.Resolve<PomodoroEngineSettings>();
            if (!string.IsNullOrEmpty(settings.ReleaseNotes))
            {
                var parent = (Window)sender;
                var releaseNoteWindow = new ReleaseNotesWindow(settings.ReleaseNotes) { Owner = parent };
                releaseNoteWindow.ShowDialog();
                settings.ReleaseNotes = null;
            }
        }

        private static void LoadSnapshot()
        {
            var engine = Container.Resolve<IPomodoroEngine>();
            var date = Container.Resolve<IDate>();
            var json = Container.Resolve<IJson>();

            try
            {
                var file = SnapshotFile();

                if (!File.Exists(file))
                {
                    return;
                }

                var snapshotJson = File.ReadAllText(file);
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
                if ((snapshot.Phase == PomodoroPhase.Work || snapshot.Phase == PomodoroPhase.Pause)
                    && (startImmediately ||
                        MessageBox.Show(
                            $"Remaining time: {remainingTime.Minutes:00}:{remainingTime.Seconds:00}. Resume pomodoro ?",
                            "Unfinished pomodoro", MessageBoxButton.YesNo) == MessageBoxResult.Yes))
                {
                    snapshot.StartDate = date.DateTimeUtc();
                    engine.LoadSnapshot(snapshot);
                }
            }
            catch
            {
                //Ignore corrupted snapshots
            }
            finally
            {
                RemoveSnapshotFile();
            }
        }

        private static string SnapshotFile()
        {
            string baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"YAPA2");

            var file = Path.Combine(baseDir, "snapshot.json");
            return file;
        }

        private static void RemoveSnapshotFile()
        {
            try
            {
                var file = SnapshotFile();
                if (!File.Exists(file))
                {
                    return;
                }
                File.Delete(file);
            }
            catch (Exception)
            {
                //Ignore
            }
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            //Each step on its own: what made the application crash may also break logging or the single instance check
            try
            {
                LogFatal($"Unhandled exception: {e.ExceptionObject}");
            }
            catch (Exception)
            {
            }

            //Recorded now (saved to disk, sent in the background); waited for only at the end, so that
            //a failed start hands over to the previous version without waiting on the network
            try
            {
                ReportCrash(e.ExceptionObject as Exception);
            }
            catch (Exception)
            {
            }

            if (StartupRecovery.StartupCompleted)
            {
                FlushCrashReports();
                return;
            }

            try
            {
                //Otherwise the previous version would take this dying process for a running copy and exit
                ReleaseSingleInstance();
            }
            catch (Exception)
            {
            }

            var handedOver = StartupRecovery.TryStartPreviousVersion(e.ExceptionObject as Exception, LogFatal);

            //The previous version is starting meanwhile; a report not sent in time goes at the next start
            FlushCrashReports();

            if (handedOver)
            {
                Environment.Exit(1);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void FlushCrashReports()
        {
            try
            {
                CrashReporting.Flush();
            }
            catch (Exception)
            {
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void LogFatal(string message)
        {
            LogManager.GetLogger("YAPA2").Fatal(message);
        }

        //Crash reporting in methods of their own: a missing Sentry assembly must not break the start or the crash handler
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static IDisposable StartCrashReporting()
        {
            try
            {
                return CrashReporting.Start();
            }
            catch (Exception ex)
            {
                LogManager.GetLogger("YAPA2").Warn(ex, "Crash reporting could not start");
                return null;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void StopCrashReporting(IDisposable crashReporting)
        {
            try
            {
                crashReporting?.Dispose();
            }
            catch (Exception)
            {
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void AskAboutCrashReports(Window owner)
        {
            try
            {
                CrashReportWindow.ShowIfNeeded(owner, () => Container.Resolve<CrashReportingSettings>());
            }
            catch (Exception ex)
            {
                LogManager.GetLogger("YAPA2").Warn(ex, "Could not ask about crash reports");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void FollowCrashReportingSetting()
        {
            try
            {
                CrashReporting.Follow(Container.Resolve<ISettings>(), Container.Resolve<CrashReportingSettings>());
            }
            catch (Exception ex)
            {
                LogManager.GetLogger("YAPA2").Warn(ex, "Crash reporting setting is not available");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ReportCrash(Exception exception)
        {
            if (exception != null)
            {
                CrashReporting.ReportFatal(exception);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ReleaseSingleInstance()
        {
            SingleInstance<App>.Cleanup();
        }

        private static void MainWindow_Closed(object sender, EventArgs e)
        {
            Current.Shutdown();
        }

        private static async Task Update(ISettingManager settings, IEnvironment environment, PomodoroEngineSettings engineSettings)
        {
            var logger = LogManager.GetLogger("YAPA2");

            try
            {
#if RELEASE_SAFETY_TEST
                //Test builds only (Build\release-safety): a release always updates from its own server
                //Another feed (a web address or a local folder made by Squirrel --releasify), for testing releases
                var customFeed = Environment.GetEnvironmentVariable("YAPA_UPDATE_URL");
                if (!string.IsNullOrEmpty(customFeed))
                {
                    logger.Info($"Updating from: {customFeed}");
                    var customVersion = await UpdateFromUrl(customFeed);
                    logger.Info(string.IsNullOrEmpty(customVersion) ? "Up to date" : $"Installed {customVersion}, it starts next time");
                    if (!string.IsNullOrEmpty(customVersion))
                    {
                        //No release notes: test versions are not on GitHub
                        settings.RestartNeeded = true;
                        settings.NewVersion = customVersion;
                    }
                    return;
                }
#endif

                var releaseUrl = "yapa-2/";
                var preReleaseUrl = "yapa-2-pre-release/";

                var httpsUrl = "https://app.floatas.net/installers";
                var httpUrl = "http://app.floatas.net/installers";

                var updateType = environment.PreRelease() ? preReleaseUrl : releaseUrl;

                string newVersion;
                try
                {
                    var httpsUpdateUrl = CombineUri(httpsUrl, updateType);
                    logger.Info($"Updating from: {httpsUpdateUrl}");

                    newVersion = await UpdateFromUrl(httpsUpdateUrl);
                }
                catch (Exception e)
                {
                    //Keep updates working where HTTPS can't be used (old TLS setup, missing root certificate, proxy)
                    var httpUpdateUrl = CombineUri(httpUrl, updateType);
                    logger.Warn(e, $"Update over HTTPS failed, falling back to: {httpUpdateUrl}");

                    newVersion = await UpdateFromUrl(httpUpdateUrl);
                }

                UpdateSettingsWithReleaseInfo(newVersion, settings, engineSettings);
            }
            catch (Exception e)
            {
                logger.Fatal(e, $"Unhandled exception while updating");
            }
        }

        private static string CombineUri(params string[] parts)
        {
            return string.Join("/", parts);
        }

        private static void UpdateSettingsWithReleaseInfo(string newVersion, ISettingManager settings, PomodoroEngineSettings engineSettings)
        {
            if (string.IsNullOrEmpty(newVersion))
            {
                return;
            }
            settings.RestartNeeded = true;
            settings.NewVersion = newVersion;
            engineSettings.ReleaseNotes = GetReleaseNotesFor(newVersion);
        }

        private static async Task<string> UpdateFromUrl(string updateUrl)
        {
            var version = string.Empty;
            using (var mgr = new UpdateManager(updateUrl))
            {
                var update = await mgr.UpdateApp();
                if (!string.IsNullOrEmpty(update?.Filename))
                {
                    version = update.Version.ToString();
                }
            }

            return version;
        }

        private static string GetReleaseNotesFor(string newVersion)
        {
            var reader = new FeedReader(new RssFeedNormalizer());
            var releases = reader.RetrieveFeed("https://github.com/YetAnotherPomodoroApp/YAPA-2/releases.atom");
            var release = releases.First(x => x.Title.Contains(newVersion));
            return release?.Content;
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

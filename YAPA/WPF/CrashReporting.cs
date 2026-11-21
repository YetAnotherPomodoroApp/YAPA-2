using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using NLog;
using Sentry;
using YAPA.Shared.Contracts;

namespace YAPA.WPF
{
    /// <summary>
    /// Sends crash reports to a Sentry-compatible server (for example a self-hosted Bugsink).
    /// A report has the error, its stack trace, the YAPA and Windows versions and the last log
    /// lines before it; never the computer name or the Windows user name. Reports are written to
    /// Documents\YAPA2\crash-reports first, so one from a crash, or while offline, is sent at the
    /// next start. Users can turn it off on the About page.
    /// </summary>
    public static class CrashReporting
    {
        //The server's DSN; empty until a public server is set up. YAPA_CRASH_DSN overrides it.
        private const string DefaultDsn = "";

        private static readonly string BaseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "YAPA2");

        public static bool IsActive { get; private set; }

        //Read from settings.json at start (before the settings service exists) and kept in step after
        private static volatile bool _sendAllowed = true;

        public static IDisposable Start()
        {
            var dsn = Environment.GetEnvironmentVariable("YAPA_CRASH_DSN");
            if (string.IsNullOrEmpty(dsn))
            {
                dsn = DefaultDsn;
            }
            if (string.IsNullOrEmpty(dsn))
            {
                return null;
            }

            _sendAllowed = ReadSettingFromFile();
            var version = Assembly.GetExecutingAssembly().GetName().Version.ToString(3);

            var sdk = SentrySdk.Init(o =>
            {
                o.Dsn = dsn;
                o.Release = $"yapa@{version}";
                o.Environment = File.Exists(Path.Combine(BaseDir, "PreRelease.txt")) ? "pre-release" : "release";
                o.IsGlobalModeEnabled = true;
                o.AutoSessionTracking = true;
                o.CacheDirectoryPath = Path.Combine(BaseDir, "crash-reports");
                //Don't wait at start to send saved reports: they go in the background. Waiting held up a
                //start, and at a crash with no network it delayed the hand-over to the previous version
                o.InitCacheFlushTimeout = TimeSpan.Zero;
                o.SendDefaultPii = false;
                o.ServerName = string.Empty;
                o.AttachStacktrace = true;
                o.MaxBreadcrumbs = 50;
                o.SetBeforeSend((report, hint) => _sendAllowed ? Scrub(report) : null);
                o.SetBeforeBreadcrumb((breadcrumb, hint) => Scrub(breadcrumb));
            });

            //The log lines before an error become its breadcrumbs; the log itself sends nothing
            var configuration = LogManager.Configuration;
            if (configuration != null)
            {
                var target = new BreadcrumbTarget { Name = "crash-report-breadcrumbs" };
                configuration.AddTarget(target);
                //Not Fatal: that is the crash itself, which the report already carries
                configuration.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Error, target);
                LogManager.Configuration = configuration;
            }

            IsActive = true;
            return sdk;
        }

        /// <summary>Follows the "Send crash reports" setting while the app runs.</summary>
        public static void Follow(ISettings settings, CrashReportingSettings crashSettings)
        {
            _sendAllowed = crashSettings.SendCrashReports;
            settings.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == $"{CrashReportingSettings.Component}.{nameof(CrashReportingSettings.SendCrashReports)}")
                {
                    _sendAllowed = crashSettings.SendCrashReports;
                }
            };
        }

        /// <summary>For errors the app survives but that should still be reported.</summary>
        public static void Report(Exception exception)
        {
            if (IsActive)
            {
                SentrySdk.CaptureException(exception);
            }
        }

        /// <summary>
        /// Called when the app is about to die: records the report, which is saved to the crash-reports
        /// folder and sent in the background. Doesn't wait for the network; call Flush before exiting.
        /// </summary>
        public static void ReportFatal(Exception exception)
        {
            if (!IsActive)
            {
                //A crash before crash reporting was started (a release whose start can't even be
                //compiled): start it now, for this one report
                Start();
            }
            if (!IsActive)
            {
                return;
            }
            //Shown as a crash ("unhandled"), not as an error the app caught and survived
            exception.Data[Sentry.Protocol.Mechanism.HandledKey] = false;
            exception.Data[Sentry.Protocol.Mechanism.MechanismKey] = "AppDomain.UnhandledException";
            SentrySdk.CaptureException(exception, scope => scope.Level = SentryLevel.Fatal);
        }

        /// <summary>
        /// Gives recorded reports up to three seconds to leave before the process ends. What doesn't
        /// leave in time stays in the crash-reports folder and is sent at the next start.
        /// </summary>
        public static void Flush()
        {
            if (IsActive)
            {
                SentrySdk.Flush(TimeSpan.FromSeconds(3));
            }
        }

        private static bool ReadSettingFromFile()
        {
            try
            {
                var file = Path.Combine(BaseDir, "settings.json");
                if (!File.Exists(file))
                {
                    return true;
                }
                var value = JObject.Parse(File.ReadAllText(file))[CrashReportingSettings.Component]?[nameof(CrashReportingSettings.SendCrashReports)];
                return value == null || value.Value<bool>();
            }
            catch
            {
                return true;
            }
        }

        private static readonly string UserName = Environment.UserName;

        private static string ScrubText(string text)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(UserName))
            {
                return text;
            }
            return text.Replace($"\\{UserName}\\", "\\<user>\\").Replace($"/{UserName}/", "/<user>/").Replace(UserName, "<user>");
        }

        private static SentryEvent Scrub(SentryEvent report)
        {
            report.ServerName = null;
            if (report.Message?.Formatted != null)
            {
                report.Message.Formatted = ScrubText(report.Message.Formatted);
            }
            foreach (var exception in report.SentryExceptions ?? Enumerable.Empty<Sentry.Protocol.SentryException>())
            {
                exception.Value = ScrubText(exception.Value);
            }
            return report;
        }

        private static Breadcrumb Scrub(Breadcrumb breadcrumb)
        {
            return new Breadcrumb(ScrubText(breadcrumb.Message), breadcrumb.Type, breadcrumb.Data, breadcrumb.Category, breadcrumb.Level);
        }
    }

    /// <summary>Turns log lines into crash report breadcrumbs (Sentry.NLog needs NLog 5).</summary>
    public sealed class BreadcrumbTarget : NLog.Targets.TargetWithLayout
    {
        public BreadcrumbTarget()
        {
            Layout = "${message}${onexception:inner= ${exception:format=type,message}}";
        }

        protected override void Write(LogEventInfo logEvent)
        {
            var level = logEvent.Level >= NLog.LogLevel.Error ? BreadcrumbLevel.Error
                : logEvent.Level == NLog.LogLevel.Warn ? BreadcrumbLevel.Warning
                : BreadcrumbLevel.Info;
            SentrySdk.AddBreadcrumb(RenderLogEvent(Layout, logEvent), logEvent.LoggerName, "log", level: level);
        }
    }

    public class CrashReportingSettings : IPluginSettings
    {
        public const string Component = "CrashReporting";

        private readonly ISettingsForComponent _settings;

        public bool SendCrashReports
        {
            get => _settings.Get(nameof(SendCrashReports), true);
            set => _settings.Update(nameof(SendCrashReports), value);
        }

        public CrashReportingSettings(ISettings settings)
        {
            _settings = settings.GetSettingsForComponent(Component);
        }

        public void DeferChanges()
        {
            _settings.DeferChanges();
        }
    }
}

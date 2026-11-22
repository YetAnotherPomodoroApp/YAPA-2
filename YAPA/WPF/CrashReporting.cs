using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using Sentry;
using Sentry.Extensibility;
using Sentry.Protocol.Envelopes;
using YAPA.Shared.Contracts;

namespace YAPA.WPF
{
    public enum CrashReportMode
    {
        //Show the report at the next start and let the user decide
        Ask,
        Always,
        Never
    }

    /// <summary>
    /// Crash reports for a Sentry-compatible server (a self-hosted Bugsink). A report has the error, its
    /// stack trace, the YAPA, Windows and .NET versions and the last log lines before it; never the
    /// computer name, the Windows user name, settings or history.
    ///
    /// Nothing leaves without the user's say: a report is saved to Documents\YAPA2\crash-reports\pending.
    /// With "Ask" (the default) the next start shows what it contains and the user sends it or not
    /// (CrashReportWindow); with "Always" it is sent right away; with "Never" none is made.
    /// </summary>
    public static class CrashReporting
    {
        //The project's address on our Bugsink (Oracle server, CapRover app "bugsink"). bugs.floatas.net is
        //ours, so the server can move without changing installed copies. The key only lets reports in.
        private const string DefaultDsn = "https://22d64ddfd00f4df8b72f6eb0814a0635@bugs.floatas.net/1";

        private static readonly string BaseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "YAPA2");

        public static bool IsActive { get; private set; }

        public static PendingReports Pending { get; private set; }

        //Read from settings.json at start (before the settings service exists) and kept in step after
        private static volatile int _mode = (int)CrashReportMode.Ask;

        public static CrashReportMode Mode => (CrashReportMode)_mode;

        private static string Dsn()
        {
#if RELEASE_SAFETY_TEST
            //Test builds only (Build\release-safety): a release can't be pointed at another server
            var custom = Environment.GetEnvironmentVariable("YAPA_CRASH_DSN");
            if (!string.IsNullOrEmpty(custom))
            {
                return custom;
            }
#endif
            return DefaultDsn;
        }

        public static IDisposable Start()
        {
            var dsn = Dsn();
            if (string.IsNullOrEmpty(dsn))
            {
                return null;
            }

            _mode = (int)ReadModeFromFile();
            var version = Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
            Pending = new PendingReports(Path.Combine(BaseDir, "crash-reports", "pending"), dsn, version);

            var sdk = SentrySdk.Init(o =>
            {
                o.Dsn = dsn;
                o.Release = $"yapa@{version}";
                o.Environment = File.Exists(Path.Combine(BaseDir, "PreRelease.txt")) ? "pre-release" : "release";
                o.IsGlobalModeEnabled = true;
                //No usage sessions: only crash reports, and only those the user agrees to
                o.AutoSessionTracking = false;
                o.Transport = new ConsentTransport(Pending);
                o.SendDefaultPii = false;
                o.ServerName = string.Empty;
                o.AttachStacktrace = true;
                o.MaxBreadcrumbs = 50;
                o.SetBeforeSend((report, hint) => Mode == CrashReportMode.Never ? null : Scrub(report));
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

        /// <summary>Follows the crash report setting while the app runs.</summary>
        public static void Follow(ISettings settings, CrashReportingSettings crashSettings)
        {
            _mode = (int)crashSettings.Mode;
            settings.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName != null && e.PropertyName.StartsWith($"{CrashReportingSettings.Component}."))
                {
                    _mode = (int)crashSettings.Mode;
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
        /// Called when the app is about to die: records the report (saved to disk; sent right away only
        /// with "Always"). Doesn't wait; call Flush before exiting.
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
        /// Gives recorded reports up to three seconds to be saved (and, with "Always", sent) before the
        /// process ends. One not sent in time is sent at the next start.
        /// </summary>
        public static void Flush()
        {
            if (IsActive)
            {
                SentrySdk.Flush(TimeSpan.FromSeconds(3));
            }
        }

        /// <summary>
        /// At start: sends, in the background, the reports the user already agreed to that could not be
        /// sent before, and with "Always" also those waiting.
        /// </summary>
        public static void SendWaitingInBackground()
        {
            var pending = Pending;
            if (!IsActive || pending == null || Mode == CrashReportMode.Never)
            {
                return;
            }
            var reports = pending.Approved().ToList();
            if (Mode == CrashReportMode.Always)
            {
                reports.AddRange(pending.List());
            }
            if (!reports.Any())
            {
                return;
            }
            Task.Run(async () =>
            {
                foreach (var report in reports)
                {
                    await pending.SendAsync(report).ConfigureAwait(false);
                }
            });
        }

        private static CrashReportMode ReadModeFromFile()
        {
            try
            {
                var file = Path.Combine(BaseDir, "settings.json");
                if (!File.Exists(file))
                {
                    return CrashReportMode.Ask;
                }
                return CrashReportingSettings.ParseMode(JObject.Parse(File.ReadAllText(file))[CrashReportingSettings.Component] as JObject);
            }
            catch
            {
                return CrashReportMode.Ask;
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

        /// <summary>Saves each report instead of sending it; sends right away only with "Always".</summary>
        private sealed class ConsentTransport : ITransport
        {
            private readonly PendingReports _pending;

            public ConsentTransport(PendingReports pending)
            {
                _pending = pending;
            }

            public async Task SendEnvelopeAsync(Envelope envelope, CancellationToken cancellationToken = default)
            {
                if (envelope.TryGetEventId() == null || Mode == CrashReportMode.Never)
                {
                    return;
                }
                var report = _pending.Save(envelope);
                if (report != null && Mode == CrashReportMode.Always)
                {
                    await _pending.SendAsync(report).ConfigureAwait(false);
                }
            }
        }
    }

    /// <summary>Reports saved on disk until they are sent or the user declines them.</summary>
    public sealed class PendingReports
    {
        private readonly string _folder;
        private readonly Uri _endpoint;
        private readonly string _auth;
        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };

        public PendingReports(string folder, string dsn, string version)
        {
            _folder = folder;
            //http(s)://<key>@<host>/<project> -> http(s)://<host>/api/<project>/envelope/
            var uri = new Uri(dsn);
            var project = uri.AbsolutePath.Trim('/').Split('/').Last();
            var prefix = uri.AbsolutePath.Trim('/');
            prefix = prefix.Length > project.Length ? "/" + prefix.Substring(0, prefix.Length - project.Length - 1) : "";
            _endpoint = new Uri($"{uri.Scheme}://{uri.Authority}{prefix}/api/{project}/envelope/");
            _auth = $"Sentry sentry_version=7, sentry_client=yapa/{version}, sentry_key={uri.UserInfo.Split(':')[0]}";
        }

        public string Save(Envelope envelope)
        {
            try
            {
                Directory.CreateDirectory(_folder);
                var file = Path.Combine(_folder, $"{envelope.TryGetEventId()}.envelope");
                using (var stream = File.Create(file))
                {
                    envelope.Serialize(stream, null);
                }
                return file;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Reports waiting for the user's answer.</summary>
        public IReadOnlyList<string> List()
        {
            return Directory.Exists(_folder)
                ? Directory.GetFiles(_folder, "*.envelope").OrderBy(File.GetLastWriteTimeUtc).ToList()
                : new List<string>();
        }

        private string ApprovedFolder => Path.Combine(_folder, "approved");

        /// <summary>Reports the user agreed to send that could not be sent yet (no network).</summary>
        public IReadOnlyList<string> Approved()
        {
            return Directory.Exists(ApprovedFolder)
                ? Directory.GetFiles(ApprovedFolder, "*.envelope").ToList()
                : new List<string>();
        }

        /// <summary>Marks a report as agreed to, so a later start sends it without asking again.</summary>
        public string Approve(string file)
        {
            try
            {
                Directory.CreateDirectory(ApprovedFolder);
                var target = Path.Combine(ApprovedFolder, Path.GetFileName(file));
                if (File.Exists(target))
                {
                    File.Delete(target);
                }
                File.Move(file, target);
                return target;
            }
            catch
            {
                return file;
            }
        }

        /// <summary>Sends a saved report; deletes it once the server has it.</summary>
        public async Task<bool> SendAsync(string file)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, _endpoint)
                {
                    Content = new ByteArrayContent(File.ReadAllBytes(file))
                };
                request.Content.Headers.TryAddWithoutValidation("Content-Type", "application/x-sentry-envelope");
                request.Headers.TryAddWithoutValidation("X-Sentry-Auth", _auth);
                using (var response = await Http.SendAsync(request).ConfigureAwait(false))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        return false;
                    }
                }
                Delete(file);
                return true;
            }
            catch
            {
                //no network: it stays, and goes at a later start
                return false;
            }
        }

        public void Delete(string file)
        {
            try
            {
                File.Delete(file);
            }
            catch
            {
                //gone already
            }
        }

        /// <summary>What a saved report contains, in plain words, and the whole of it.</summary>
        public static CrashReportSummary Read(string file)
        {
            var summary = new CrashReportSummary { File = file };
            var lines = File.ReadAllLines(file, Encoding.UTF8);
            //An envelope: a header line, then for each item a header line and its payload
            for (var i = 1; i + 1 < lines.Length; i += 2)
            {
                var header = JObject.Parse(lines[i]);
                if ((string)header["type"] != "event")
                {
                    continue;
                }
                var report = JObject.Parse(lines[i + 1]);
                summary.Full = report.ToString(Formatting.Indented);
                summary.When = (DateTime?)report["timestamp"];

                var exception = (report["exception"]?["values"] as JArray)?.LastOrDefault();
                if (exception != null)
                {
                    summary.Error = $"{exception["type"]}: {exception["value"]}";
                    var frames = (exception["stacktrace"]?["frames"] as JArray)?.Reverse().Take(4) ?? Enumerable.Empty<JToken>();
                    summary.Where = frames
                        .Select(f => $"{f["function"]}" + (f["filename"] != null ? $" ({f["filename"]}, line {f["lineno"]})" : ""))
                        .ToList();
                }

                var os = report["contexts"]?["os"];
                var runtime = report["contexts"]?["runtime"];
                summary.Versions = $"YAPA {((string)report["release"])?.Replace("yapa@", "")}" +
                                   $", {os?["raw_description"] ?? os?["name"]}" +
                                   $", {runtime?["name"]} {runtime?["version"]}";

                //Either a list or {"values": [...]}, depending on the SDK
                var breadcrumbToken = report["breadcrumbs"];
                var breadcrumbs = breadcrumbToken as JArray ?? (breadcrumbToken as JObject)?["values"] as JArray;
                summary.LastActions = (breadcrumbs ?? new JArray())
                    .Select(b => (string)b["message"])
                    .Where(m => !string.IsNullOrEmpty(m))
                    .Reverse().Take(5).Reverse()
                    .ToList();
                break;
            }
            return summary;
        }
    }

    public sealed class CrashReportSummary
    {
        public string File { get; set; }
        public DateTime? When { get; set; }
        public string Error { get; set; } = "";
        public IList<string> Where { get; set; } = new List<string>();
        public string Versions { get; set; } = "";
        public IList<string> LastActions { get; set; } = new List<string>();
        public string Full { get; set; } = "";
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

        public CrashReportMode Mode
        {
            get => ParseMode(_settings.Get<string>(nameof(Mode), null), _settings.Get<bool?>("SendCrashReports", null));
            set => _settings.Update(nameof(Mode), value.ToString());
        }

        public CrashReportingSettings(ISettings settings)
        {
            _settings = settings.GetSettingsForComponent(Component);
        }

        internal static CrashReportMode ParseMode(JObject component)
        {
            return ParseMode((string)component?[nameof(Mode)], (bool?)component?["SendCrashReports"]);
        }

        //"Mode" since the choice was added; an earlier "SendCrashReports": false means Never
        private static CrashReportMode ParseMode(string mode, bool? sendCrashReports)
        {
            if (Enum.TryParse(mode, true, out CrashReportMode parsed))
            {
                return parsed;
            }
            return sendCrashReports == false ? CrashReportMode.Never : CrashReportMode.Ask;
        }

        /// <summary>
        /// Stores the choice made in the crash report window. Not deferred, so it is written straight
        /// away; the window shows at start, before any settings page can hold unsaved changes.
        /// </summary>
        public void Remember(CrashReportMode mode)
        {
            Mode = mode;
        }

        public void DeferChanges()
        {
            _settings.DeferChanges();
        }
    }
}

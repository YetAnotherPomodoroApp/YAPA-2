using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using NLog;

namespace YAPA.WPF
{
    /// <summary>
    /// At the start after a crash: shows what the crash report contains and lets the user send it
    /// or not. Only with "Ask" (the default); "Always" sends without asking, "Never" makes none.
    /// </summary>
    public partial class CrashReportWindow
    {
        public bool Send { get; private set; }

        public CrashReportWindow(CrashReportSummary summary, int earlierReports)
        {
            InitializeComponent();

            if (summary.When.HasValue)
            {
                Heading.Text = $"YAPA closed unexpectedly on {summary.When.Value.ToLocalTime():g}.";
            }
            Error.Text = summary.Error;
            Where.Text = summary.Where.Any() ? string.Join(Environment.NewLine, summary.Where) : "-";
            Versions.Text = summary.Versions;
            LastSteps.Text = summary.LastActions.Any() ? string.Join(Environment.NewLine, summary.LastActions) : "-";
            FullReport.Text = summary.Full;

            if (earlierReports > 0)
            {
                MoreReports.Text = earlierReports == 1
                    ? "There is one more report, from an earlier time; your choice is for both."
                    : $"There are {earlierReports} more reports, from earlier times; your choice is for all of them.";
                MoreReports.Visibility = Visibility.Visible;
            }
        }

        private void Send_Click(object sender, RoutedEventArgs e)
        {
            Send = true;
            Close();
        }

        private void DontSend_Click(object sender, RoutedEventArgs e)
        {
            Send = false;
            Close();
        }

        /// <summary>
        /// Called once the main window is up. With "Ask" and saved reports, shows the newest and does
        /// what the user chose with all of them; with "Always" sends them in the background.
        /// </summary>
        public static void ShowIfNeeded(Window owner, Func<CrashReportingSettings> settings)
        {
            var pending = CrashReporting.Pending;
            if (!CrashReporting.IsActive || pending == null)
            {
                return;
            }

            //Agreed to earlier but not sent then (no network), and with "Always" everything waiting
            CrashReporting.SendWaitingInBackground();

            var reports = pending.List();
            if (!reports.Any())
            {
                return;
            }

            switch (CrashReporting.Mode)
            {
                case CrashReportMode.Always:
                    return;
                case CrashReportMode.Never:
                    foreach (var report in reports)
                    {
                        pending.Delete(report);
                    }
                    return;
            }

            CrashReportSummary summary;
            try
            {
                summary = PendingReports.Read(reports.Last());
            }
            catch (Exception ex)
            {
                //A report we can't show is not sent
                LogManager.GetLogger("YAPA2").Warn(ex, "A saved crash report could not be read; it is removed");
                pending.Delete(reports.Last());
                return;
            }

            var window = new CrashReportWindow(summary, reports.Count - 1);
            if (owner != null && owner.IsVisible)
            {
                window.Owner = owner;
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            window.ShowDialog();

            if (window.Send)
            {
                //Agreed to: if there is no network now, a later start sends them without asking again
                var approved = reports.Select(pending.Approve).ToList();
                Task.Run(async () =>
                {
                    foreach (var report in approved)
                    {
                        await pending.SendAsync(report).ConfigureAwait(false);
                    }
                });
            }
            else
            {
                foreach (var report in reports)
                {
                    pending.Delete(report);
                }
            }

            if (window.RememberChoice.IsChecked == true)
            {
                settings().Remember(window.Send ? CrashReportMode.Always : CrashReportMode.Never);
            }

            LogManager.GetLogger("YAPA2").Info($"Crash report: {(window.Send ? "sent" : "not sent")}{(window.RememberChoice.IsChecked == true ? ", choice remembered" : "")}");
        }
    }
}

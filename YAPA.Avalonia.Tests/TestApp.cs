using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Avalonia.Threading;
using YAPA.Avalonia.Infrastructure;
using YAPA.Shared.Contracts;
using Xunit;

[assembly: AvaloniaTestApplication(typeof(YAPA.Avalonia.Tests.TestApp))]
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]

namespace YAPA.Avalonia.Tests
{
    public static class TestApp
    {
        public static string Home { get; } = Path.Combine(Path.GetTempPath(), "yapa-ui-tests-" + Environment.ProcessId);

        // Before anything reads AvaloniaEnvironment.BaseDir, so tests never touch Documents/YAPA2
        [ModuleInitializer]
        public static void UseTestHome()
        {
            Environment.SetEnvironmentVariable("YAPA_HOME", Home);
        }

        public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
    }

    /// <summary>A fresh YAPA: empty data folder (or the settings given), container, timer window shown.</summary>
    public sealed class Yapa : IDisposable
    {
        public DependencyContainer App { get; }
        public Window Timer => App.MainWindow;

        public FakeClock Clock { get; } = new FakeClock();
        public FakeSound Sound { get; } = new FakeSound();

        public Yapa(string settingsJson = null)
        {
            // Pooled connections would keep writing to the previous test's deleted database
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (Directory.Exists(TestApp.Home))
            {
                Directory.Delete(TestApp.Home, true);
            }
            Directory.CreateDirectory(TestApp.Home);
            if (settingsJson != null)
            {
                File.WriteAllText(Path.Combine(TestApp.Home, "settings.json"), settingsJson);
            }
            App = new DependencyContainer(di =>
            {
                di.RegisterInstance(Clock, typeof(IDate));
                di.RegisterInstance(Clock, typeof(ITimer));
                di.RegisterInstance(Sound, typeof(IMusicPlayer));
            });
            Timer.Show();
            Pump();
        }

        public T Resolve<T>() => App.Container.Resolve<T>();

        public Settings.SettingsWindow SettingsWindow => Resolve<Settings.SettingsWindow>();

        /// <summary>Opens settings as a right click on the timer does, on the page with this title.</summary>
        public Control OpenSettings(string page)
        {
            var command = Resolve<IMainViewModel>().ShowSettingsCommand;
            if (!SettingsWindow.IsVisible)
            {
                command.Execute(null);
                Pump();
            }
            var tree = SettingsWindow.Find<TreeView>("SettingsTree");
            var item = tree.GetLogicalDescendants().OfType<TreeViewItem>()
                .First(x => (x.Header as TextBlock)?.Text == page);
            if (item.Parent is TreeViewItem parent)
            {
                parent.IsExpanded = true;
            }
            tree.SelectedItem = item;
            Pump();
            return (Control)item.Tag;
        }

        public void Save()
        {
            var save = SettingsWindow.Find<Button>("SaveButton");
            Assert.True(save.IsEffectivelyEnabled, "Save should be enabled after a change");
            Click(save);
        }

        /// <summary>A real mouse click in the middle of the control.</summary>
        public static void Click(Control control)
        {
            // scrolled into view first, as a person would
            control.BringIntoView();
            Pump();
            var top = (Window)TopLevel.GetTopLevel(control);
            // a check box is clicked on its box, not in the empty end of a wide row
            var x = control is CheckBox ? 10 : control.Bounds.Width / 2;
            var centre = control.TranslatePoint(new Point(x, control.Bounds.Height / 2), top).Value;
            top.MouseDown(centre, global::Avalonia.Input.MouseButton.Left);
            top.MouseUp(centre, global::Avalonia.Input.MouseButton.Left);
            Pump();
        }

        public ISettings Settings => Resolve<ISettings>();

        public string SettingsFile => File.ReadAllText(Path.Combine(TestApp.Home, "settings.json"));

        /// <summary>Lets bindings, layout and rendering catch up.</summary>
        public static void Pump()
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
        }

        /// <summary>Keeps the UI running for this long in real time (delays, animations, slider Delay=300).</summary>
        public static void Pump(int milliseconds)
        {
            // Run the dispatcher's own loop, so its timers fire on real time
            using (var cancel = new System.Threading.CancellationTokenSource(milliseconds))
            {
                Dispatcher.UIThread.MainLoop(cancel.Token);
            }
            Pump();
        }

        /// <summary>Types into a text box or an editable combo box, replacing what is there.</summary>
        public static void Type(Control control, string text)
        {
            var box = control as TextBox ?? control.GetVisualDescendants().OfType<TextBox>().First();
            control.BringIntoView();
            box.Focus();
            box.SelectAll();
            Pump();
            var top = (Window)TopLevel.GetTopLevel(box);
            top.KeyPressQwerty(global::Avalonia.Input.PhysicalKey.Delete, global::Avalonia.Input.RawInputModifiers.None);
            top.KeyReleaseQwerty(global::Avalonia.Input.PhysicalKey.Delete, global::Avalonia.Input.RawInputModifiers.None);
            top.KeyTextInput(text);
            Pump();
        }

        public bool SaveEnabled => SettingsWindow.Find<Button>("SaveButton").IsEffectivelyEnabled;

        /// <summary>The pointer leaving the window, which headless input can't produce by moving.</summary>
        public static void PointerLeaves(Window window)
        {
            var pointer = new global::Avalonia.Input.Pointer(global::Avalonia.Input.Pointer.GetNextFreeId(), global::Avalonia.Input.PointerType.Mouse, true);
            var args = new global::Avalonia.Input.PointerEventArgs(global::Avalonia.Input.InputElement.PointerExitedEvent, window, pointer, window,
                new Point(-10, -10), 0, new global::Avalonia.Input.PointerPointProperties(), global::Avalonia.Input.KeyModifiers.None);
            window.RaiseEvent(args);
            Pump();
        }

        public void Dispose()
        {
            try { SettingsWindow.CloseForGood(); } catch { }
            try { Resolve<IPomodoroEngine>().Stop(); } catch { }
            try { Timer.Close(); } catch { }
            Pump();
        }
    }

    public static class ControlExtensions
    {
        public static T Find<T>(this ILogical root, string name) where T : Control
        {
            var found = root.GetLogicalDescendants().OfType<T>().FirstOrDefault(x => x.Name == name);
            return found ?? throw new InvalidOperationException($"No {typeof(T).Name} named {name}");
        }

        public static CheckBox CheckBox(this ILogical root, string text) =>
            root.FindFirst<CheckBox>(x => (x.Content as string) == text);

        public static T FindFirst<T>(this ILogical root, Func<T, bool> match = null) where T : Control
        {
            var found = root.GetLogicalDescendants().OfType<T>().FirstOrDefault(x => match == null || match(x));
            return found ?? throw new InvalidOperationException($"No matching {typeof(T).Name}");
        }
    }
}

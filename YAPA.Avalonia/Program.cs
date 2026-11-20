using System;
using Avalonia;
using YAPA.Avalonia.Infrastructure;

namespace YAPA.Avalonia
{
    public static class Program
    {
        public static SingleInstance Instance { get; private set; }

        [STAThread]
        public static int Main(string[] args)
        {
            // Velopack's install/update steps run here and exit; a pending update is applied first
            Velopack.VelopackApp.Build().Run();

            using (Instance = new SingleInstance())
            {
                if (!Instance.TryAcquire(args))
                {
                    // YAPA is running and has our command line ("/start", "/settings", ...)
                    return 0;
                }
                Instance.Listen();

                return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace();
    }
}

using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace YAPA.Avalonia.Infrastructure
{
    /// <summary>
    /// One YAPA per user. A second start hands its command line ("/start", "/settings", ...)
    /// to the running one and exits, as YAPA/SingleInstance.cs does with .NET remoting.
    /// </summary>
    public sealed class SingleInstance : IDisposable
    {
        // A copy with its own data folder (YAPA_HOME) is a separate YAPA and runs alongside
        private readonly string _name = "YAPA2-" + Environment.UserName + HomeSuffix();

        private static string HomeSuffix()
        {
            var home = Environment.GetEnvironmentVariable("YAPA_HOME");
            if (string.IsNullOrEmpty(home))
            {
                return string.Empty;
            }
            var hash = 17;
            foreach (var c in home)
            {
                hash = unchecked(hash * 31 + c);
            }
            return "-" + ((uint)hash).ToString("x8");
        }
        private Mutex _mutex;
        private CancellationTokenSource _cancel;

        public event Action<string[]> ArgumentsReceived;

        /// <summary>
        /// True when this is the first YAPA. Otherwise the arguments have been passed to the
        /// running one, over the same connection that found it.
        /// </summary>
        public bool TryAcquire(string[] args)
        {
            if (OperatingSystem.IsWindows())
            {
                _mutex = new Mutex(true, "Local\\" + _name, out var createdNew);
                if (createdNew)
                {
                    return true;
                }
                Send(args, 2000);
                return false;
            }

            // Named mutexes do not span processes on macOS and Linux: if somebody answers on
            // the pipe, YAPA is already running
            return !Send(args, 300);
        }

        private bool Send(string[] args, int timeout)
        {
            try
            {
                using (var client = new NamedPipeClientStream(".", _name, PipeDirection.Out))
                {
                    client.Connect(timeout);
                    using (var writer = new StreamWriter(client))
                    {
                        writer.Write(string.Join("\n", args));
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Listen()
        {
            _cancel = new CancellationTokenSource();
            var token = _cancel.Token;
            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        using (var server = new NamedPipeServerStream(_name, PipeDirection.In, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
                        {
                            await server.WaitForConnectionAsync(token);
                            using (var reader = new StreamReader(server))
                            {
                                var text = await reader.ReadToEndAsync();
                                ArgumentsReceived?.Invoke(text.Split('\n', StringSplitOptions.RemoveEmptyEntries));
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                    catch
                    {
                        await Task.Delay(200);
                    }
                }
            });
        }

        public void Dispose()
        {
            _cancel?.Cancel();
            _mutex?.Dispose();
        }
    }
}

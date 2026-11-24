using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using YAPA.Shared.Contracts;

namespace YAPA.Avalonia.Specifics
{
    /// <summary>
    /// Plays sound files without extra libraries: MCI on Windows (no Windows Media Player needed),
    /// afplay on macOS, and paplay / ffplay / aplay on Linux.
    /// Like the WPF player it keeps one sound at a time and can repeat it.
    /// </summary>
    public class SoundPlayer : IMusicPlayer
    {
        private readonly object _lock = new object();
        private string _path;
        private bool _repeat;
        private double _volume;
        private Process _process;
        private string _mciAlias;
        private int _generation;

        public bool IsPlaying { get; private set; }

        public static string ResolvePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }
            if (File.Exists(path))
            {
                return path;
            }
            // Settings written on Windows use backslashes
            var normalized = path.Replace('\\', Path.DirectorySeparatorChar);
            if (File.Exists(normalized))
            {
                return normalized;
            }
            // A default sound saved by another install: look for the same file next to this app
            var bundled = Path.Combine(AppContext.BaseDirectory, "Resources", Path.GetFileName(normalized));
            return File.Exists(bundled) ? bundled : null;
        }

        public void Load(string path)
        {
            if (IsPlaying)
            {
                return;
            }
            _path = ResolvePath(path);
        }

        public void Play(bool repeat = false, double volume = 0.5)
        {
            lock (_lock)
            {
                if (_path == null || IsPlaying)
                {
                    return;
                }
                _repeat = repeat;
                _volume = Math.Max(0, Math.Min(1, volume));
                IsPlaying = true;
                _generation++;
                try
                {
                    if (OperatingSystem.IsWindows())
                    {
                        PlayWithMci();
                    }
                    else
                    {
                        StartProcess(_generation);
                    }
                }
                catch (Exception e)
                {
                    Trace.WriteLine($"YAPA: could not play '{_path}': {e.Message}");
                    IsPlaying = false;
                }
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                _generation++;
                IsPlaying = false;
                if (_mciAlias != null)
                {
                    mciSendString($"stop {_mciAlias}", null, 0, IntPtr.Zero);
                    mciSendString($"close {_mciAlias}", null, 0, IntPtr.Zero);
                    _mciAlias = null;
                }
                try
                {
                    if (_process != null && !_process.HasExited)
                    {
                        _process.Kill();
                    }
                }
                catch
                {
                    // already gone
                }
                _process = null;
            }
        }

        private void StartProcess(int generation)
        {
            var (file, args) = Command(_path, _volume);
            if (file == null)
            {
                IsPlaying = false;
                return;
            }
            var info = new ProcessStartInfo(file)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            };
            foreach (var arg in args)
            {
                info.ArgumentList.Add(arg);
            }
            var process = new Process { StartInfo = info, EnableRaisingEvents = true };
            process.Exited += (s, e) =>
            {
                lock (_lock)
                {
                    if (generation != _generation)
                    {
                        return;
                    }
                    if (_repeat && IsPlaying)
                    {
                        StartProcess(generation);
                    }
                    else
                    {
                        IsPlaying = false;
                    }
                }
            };
            process.Start();
            _process = process;
        }

        private static (string, string[]) Command(string path, double volume)
        {
            var v = volume.ToString("0.00", CultureInfo.InvariantCulture);
            if (OperatingSystem.IsMacOS())
            {
                return ("afplay", new[] { "-v", v, path });
            }
            var isWav = path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase);
            if (isWav && OnPath("paplay"))
            {
                return ("paplay", new[] { $"--volume={(int)(volume * 65536)}", path });
            }
            if (OnPath("ffplay"))
            {
                return ("ffplay", new[] { "-nodisp", "-autoexit", "-loglevel", "quiet", "-volume", ((int)(volume * 100)).ToString(), path });
            }
            if (isWav && OnPath("aplay"))
            {
                return ("aplay", new[] { "-q", path });
            }
            return (null, null);
        }

        private static bool OnPath(string program)
        {
            var paths = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator);
            foreach (var dir in paths)
            {
                if (File.Exists(Path.Combine(dir, program)))
                {
                    return true;
                }
            }
            return false;
        }

        // Windows: the MCI API plays wav and mp3 and needs no extra components
        [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
        private static extern int mciSendString(string command, StringBuilder returnValue, int returnLength, IntPtr callback);

        private void PlayWithMci()
        {
            _mciAlias = "yapa" + Guid.NewGuid().ToString("N");
            var type = _path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) ? "waveaudio" : "mpegvideo";
            if (mciSendString($"open \"{_path}\" type {type} alias {_mciAlias}", null, 0, IntPtr.Zero) != 0)
            {
                // waveaudio can't set volume or repeat; mpegvideo handles wav too on most systems
                mciSendString($"open \"{_path}\" type mpegvideo alias {_mciAlias}", null, 0, IntPtr.Zero);
            }
            mciSendString($"setaudio {_mciAlias} volume to {(int)(_volume * 1000)}", null, 0, IntPtr.Zero);
            mciSendString($"play {_mciAlias}{(_repeat ? " repeat" : string.Empty)}", null, 0, IntPtr.Zero);
        }
    }
}

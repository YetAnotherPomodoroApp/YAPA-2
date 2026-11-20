using System;
using System.IO;
using System.Runtime.InteropServices;
using YAPA.Shared.Common;

namespace YAPA.WPF.Specifics
{
    /// <summary>
    /// Plays uncompressed .wav files through the basic Windows sound API.
    /// Unlike the WPF media player it does not need Windows Media Player to be installed.
    /// Windows plays one such sound per application at a time.
    /// </summary>
    public class WavPlayer
    {
        private const uint Async = 0x0001;
        private const uint NoDefaultSound = 0x0002;
        private const uint FromMemory = 0x0004;
        private const uint Loop = 0x0008;

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern bool PlaySound(IntPtr sound, IntPtr module, uint flags);

        //The sound is played from this memory after Play returns, so it has to stay where it is until Stop
        private GCHandle _sound;

        public static bool CanPlay(string path)
        {
            return string.Equals(Path.GetExtension(path), ".wav", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Starts playing and returns at once. Throws when Windows can't play the file.
        /// </summary>
        public void Play(string path, bool repeat, double volume)
        {
            Stop();

            var wav = File.ReadAllBytes(path);
            WavVolume.Apply(wav, volume);

            _sound = GCHandle.Alloc(wav, GCHandleType.Pinned);

            var flags = Async | NoDefaultSound | FromMemory | (repeat ? Loop : 0);
            if (!PlaySound(_sound.AddrOfPinnedObject(), IntPtr.Zero, flags))
            {
                _sound.Free();
                throw new InvalidOperationException($"Windows can't play '{path}' as a .wav file");
            }
        }

        public void Stop()
        {
            if (!_sound.IsAllocated)
            {
                return;
            }

            PlaySound(IntPtr.Zero, IntPtr.Zero, 0);
            _sound.Free();
        }
    }
}

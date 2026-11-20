using System;

namespace YAPA.Shared.Common
{
    /// <summary>
    /// Changes the loudness of an uncompressed .wav file held in memory.
    /// The basic Windows sound API has no volume setting, so the samples themselves are scaled.
    /// </summary>
    public static class WavVolume
    {
        private const int FormatPcm = 1;
        private const int FormatFloat = 3;
        private const int FormatExtensible = 0xFFFE;

        /// <summary>
        /// Scales every sample of <paramref name="wav"/> by <paramref name="volume"/> (0 to 1), in place.
        /// Returns false and leaves the data untouched when the file is not uncompressed audio it understands.
        /// </summary>
        public static bool Apply(byte[] wav, double volume)
        {
            if (wav == null || wav.Length < 12 || !HasTag(wav, 0, "RIFF") || !HasTag(wav, 8, "WAVE"))
            {
                return false;
            }

            if (double.IsNaN(volume))
            {
                return false;
            }

            volume = Math.Max(0, Math.Min(1, volume));

            var format = 0;
            var bitsPerSample = 0;
            var position = 12;

            while (position + 8 <= wav.Length)
            {
                var size = BitConverter.ToUInt32(wav, position + 4);
                var start = position + 8;
                if (size > (uint)(wav.Length - start))
                {
                    //A data chunk may claim more than the file holds; anything else that does is broken
                    if (!HasTag(wav, position, "data"))
                    {
                        return false;
                    }
                    size = (uint)(wav.Length - start);
                }

                if (HasTag(wav, position, "fmt "))
                {
                    if (size < 16)
                    {
                        return false;
                    }

                    format = BitConverter.ToUInt16(wav, start);
                    bitsPerSample = BitConverter.ToUInt16(wav, start + 14);

                    //The extensible format keeps the real format in the first two bytes of its sub format
                    if (format == FormatExtensible && size >= 26)
                    {
                        format = BitConverter.ToUInt16(wav, start + 24);
                    }
                }
                else if (HasTag(wav, position, "data"))
                {
                    return Scale(wav, start, (int)size, format, bitsPerSample, volume);
                }

                position = start + (int)size + (int)(size % 2);
            }

            return false;
        }

        private static bool Scale(byte[] wav, int start, int length, int format, int bitsPerSample, double volume)
        {
            if (format == FormatPcm && bitsPerSample == 8)
            {
                //8 bit samples are unsigned with silence at 128
                for (var i = start; i < start + length; i++)
                {
                    wav[i] = (byte)Math.Round((wav[i] - 128) * volume + 128);
                }
                return true;
            }

            if (format == FormatPcm && bitsPerSample == 16)
            {
                for (var i = start; i + 1 < start + length; i += 2)
                {
                    var sample = (short)Math.Round(BitConverter.ToInt16(wav, i) * volume);
                    wav[i] = (byte)sample;
                    wav[i + 1] = (byte)(sample >> 8);
                }
                return true;
            }

            if (format == FormatPcm && bitsPerSample == 24)
            {
                for (var i = start; i + 2 < start + length; i += 3)
                {
                    var value = wav[i] | (wav[i + 1] << 8) | ((sbyte)wav[i + 2] << 16);
                    var sample = (int)Math.Round(value * volume);
                    wav[i] = (byte)sample;
                    wav[i + 1] = (byte)(sample >> 8);
                    wav[i + 2] = (byte)(sample >> 16);
                }
                return true;
            }

            if (format == FormatPcm && bitsPerSample == 32)
            {
                for (var i = start; i + 3 < start + length; i += 4)
                {
                    var sample = (int)Math.Round(BitConverter.ToInt32(wav, i) * volume);
                    Array.Copy(BitConverter.GetBytes(sample), 0, wav, i, 4);
                }
                return true;
            }

            if (format == FormatFloat && bitsPerSample == 32)
            {
                for (var i = start; i + 3 < start + length; i += 4)
                {
                    var sample = (float)(BitConverter.ToSingle(wav, i) * volume);
                    Array.Copy(BitConverter.GetBytes(sample), 0, wav, i, 4);
                }
                return true;
            }

            return false;
        }

        private static bool HasTag(byte[] data, int position, string tag)
        {
            if (position + 4 > data.Length)
            {
                return false;
            }

            for (var i = 0; i < 4; i++)
            {
                if (data[position + i] != tag[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}

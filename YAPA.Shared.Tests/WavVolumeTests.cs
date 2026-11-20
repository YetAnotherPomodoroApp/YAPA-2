using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using YAPA.Shared.Common;

namespace YAPA.Shared.Tests
{
    [TestClass]
    public class WavVolumeTests
    {
        private static byte[] Chunk(string id, byte[] content, uint? claimedSize = null)
        {
            var bytes = new List<byte>(Encoding.ASCII.GetBytes(id));
            bytes.AddRange(BitConverter.GetBytes(claimedSize ?? (uint)content.Length));
            bytes.AddRange(content);
            if (content.Length % 2 == 1)
            {
                bytes.Add(0);
            }
            return bytes.ToArray();
        }

        private static byte[] Format(ushort format, ushort bitsPerSample, ushort? subFormat = null)
        {
            ushort channels = 1;
            uint rate = 44100;
            var blockAlign = (ushort)(channels * bitsPerSample / 8);
            var bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes(format));
            bytes.AddRange(BitConverter.GetBytes(channels));
            bytes.AddRange(BitConverter.GetBytes(rate));
            bytes.AddRange(BitConverter.GetBytes(rate * blockAlign));
            bytes.AddRange(BitConverter.GetBytes(blockAlign));
            bytes.AddRange(BitConverter.GetBytes(bitsPerSample));
            if (subFormat.HasValue)
            {
                bytes.AddRange(BitConverter.GetBytes((ushort)22)); //size of the extension
                bytes.AddRange(BitConverter.GetBytes(bitsPerSample));
                bytes.AddRange(BitConverter.GetBytes((uint)4)); //channel mask
                bytes.AddRange(BitConverter.GetBytes(subFormat.Value));
                bytes.AddRange(new byte[14]); //rest of the sub format id
            }
            return bytes.ToArray();
        }

        private static byte[] Wav(params byte[][] chunks)
        {
            var body = chunks.SelectMany(x => x).ToArray();
            var bytes = new List<byte>(Encoding.ASCII.GetBytes("RIFF"));
            bytes.AddRange(BitConverter.GetBytes((uint)(body.Length + 4)));
            bytes.AddRange(Encoding.ASCII.GetBytes("WAVE"));
            bytes.AddRange(body);
            return bytes.ToArray();
        }

        private static byte[] Pcm16(params short[] samples)
        {
            return samples.SelectMany(BitConverter.GetBytes).ToArray();
        }

        private static short[] ReadPcm16(byte[] wav, int count)
        {
            var start = wav.Length - count * 2;
            return Enumerable.Range(0, count).Select(i => BitConverter.ToInt16(wav, start + i * 2)).ToArray();
        }

        [TestMethod]
        public void Pcm16_SamplesAreScaled_HeaderIsUntouched()
        {
            var wav = Wav(Chunk("fmt ", Format(1, 16)), Chunk("data", Pcm16(1000, -1000, 32767, -32768, 0, 3)));
            var original = (byte[])wav.Clone();

            Assert.IsTrue(WavVolume.Apply(wav, 0.5));

            CollectionAssert.AreEqual(new short[] { 500, -500, 16384, -16384, 0, 2 }, ReadPcm16(wav, 6));
            CollectionAssert.AreEqual(original.Take(wav.Length - 12).ToArray(), wav.Take(wav.Length - 12).ToArray());
        }

        [TestMethod]
        public void FullVolume_LeavesTheSoundAsItIs()
        {
            var wav = Wav(Chunk("fmt ", Format(1, 16)), Chunk("data", Pcm16(1000, -1000, 32767, -32768)));
            var original = (byte[])wav.Clone();

            Assert.IsTrue(WavVolume.Apply(wav, 1));

            CollectionAssert.AreEqual(original, wav);
        }

        [TestMethod]
        public void VolumeOutsideZeroToOne_IsLimited()
        {
            var loud = Wav(Chunk("fmt ", Format(1, 16)), Chunk("data", Pcm16(1000, -1000)));
            var silent = (byte[])loud.Clone();

            Assert.IsTrue(WavVolume.Apply(loud, 5));
            Assert.IsTrue(WavVolume.Apply(silent, -1));

            CollectionAssert.AreEqual(new short[] { 1000, -1000 }, ReadPcm16(loud, 2));
            CollectionAssert.AreEqual(new short[] { 0, 0 }, ReadPcm16(silent, 2));
        }

        [TestMethod]
        public void Pcm8_IsScaledAroundItsMiddleValue()
        {
            var wav = Wav(Chunk("fmt ", Format(1, 8)), Chunk("data", new byte[] { 128, 228, 28, 255, 0, 129 }));

            Assert.IsTrue(WavVolume.Apply(wav, 0.5));

            CollectionAssert.AreEqual(new byte[] { 128, 178, 78, 192, 64, 128 }, wav.Skip(wav.Length - 6).ToArray());
        }

        [TestMethod]
        public void Pcm24_IsScaled()
        {
            //100000 and -100000 as three byte samples
            var wav = Wav(Chunk("fmt ", Format(1, 24)), Chunk("data", new byte[] { 0xA0, 0x86, 0x01, 0x60, 0x79, 0xFE }));

            Assert.IsTrue(WavVolume.Apply(wav, 0.5));

            //50000 and -50000
            CollectionAssert.AreEqual(new byte[] { 0x50, 0xC3, 0x00, 0xB0, 0x3C, 0xFF }, wav.Skip(wav.Length - 6).ToArray());
        }

        [TestMethod]
        public void Pcm32AndFloat_AreScaled()
        {
            var pcm = Wav(Chunk("fmt ", Format(1, 32)), Chunk("data", BitConverter.GetBytes(1000000).Concat(BitConverter.GetBytes(-1000000)).ToArray()));
            var floating = Wav(Chunk("fmt ", Format(3, 32)), Chunk("data", BitConverter.GetBytes(0.8f).Concat(BitConverter.GetBytes(-0.4f)).ToArray()));

            Assert.IsTrue(WavVolume.Apply(pcm, 0.25));
            Assert.IsTrue(WavVolume.Apply(floating, 0.25));

            Assert.AreEqual(250000, BitConverter.ToInt32(pcm, pcm.Length - 8));
            Assert.AreEqual(-250000, BitConverter.ToInt32(pcm, pcm.Length - 4));
            Assert.AreEqual(0.2f, BitConverter.ToSingle(floating, floating.Length - 8), 0.0001f);
            Assert.AreEqual(-0.1f, BitConverter.ToSingle(floating, floating.Length - 4), 0.0001f);
        }

        [TestMethod]
        public void ExtensibleFormat_UsesItsSubFormat()
        {
            var wav = Wav(Chunk("fmt ", Format(0xFFFE, 16, 1)), Chunk("data", Pcm16(1000, -1000)));

            Assert.IsTrue(WavVolume.Apply(wav, 0.5));

            CollectionAssert.AreEqual(new short[] { 500, -500 }, ReadPcm16(wav, 2));
        }

        [TestMethod]
        public void OtherChunks_AreSkipped_AlsoWithOddLength()
        {
            var wav = Wav(
                Chunk("LIST", new byte[] { 1, 2, 3 }),
                Chunk("fmt ", Format(1, 16)),
                Chunk("fact", new byte[] { 9, 9, 9, 9, 9 }),
                Chunk("data", Pcm16(1000, -1000)));

            Assert.IsTrue(WavVolume.Apply(wav, 0.5));

            CollectionAssert.AreEqual(new short[] { 500, -500 }, ReadPcm16(wav, 2));
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, wav.Skip(20).Take(3).ToArray());
        }

        [TestMethod]
        public void DataChunkClaimingMoreThanTheFileHolds_ScalesWhatIsThere()
        {
            var wav = Wav(Chunk("fmt ", Format(1, 16)), Chunk("data", Pcm16(1000, -1000), 4000));

            Assert.IsTrue(WavVolume.Apply(wav, 0.5));

            CollectionAssert.AreEqual(new short[] { 500, -500 }, ReadPcm16(wav, 2));
        }

        [TestMethod]
        public void CompressedOrUnknownAudio_IsLeftUntouched()
        {
            var adpcm = Wav(Chunk("fmt ", Format(2, 4)), Chunk("data", new byte[] { 1, 2, 3, 4 }));
            var odd = Wav(Chunk("fmt ", Format(1, 12)), Chunk("data", new byte[] { 1, 2, 3, 4 }));
            var noFormat = Wav(Chunk("data", Pcm16(1000, -1000)));

            foreach (var wav in new[] { adpcm, odd, noFormat })
            {
                var original = (byte[])wav.Clone();

                Assert.IsFalse(WavVolume.Apply(wav, 0.5));

                CollectionAssert.AreEqual(original, wav);
            }
        }

        [TestMethod]
        public void NotAWavFile_IsLeftUntouched()
        {
            var text = Encoding.ASCII.GetBytes("this is not audio, just some text that is long enough");
            var truncated = Wav(Chunk("fmt ", Format(1, 16))).Take(20).ToArray();
            var brokenChunk = Wav(Chunk("fmt ", Format(1, 16), 4000), Chunk("data", Pcm16(1000)));

            foreach (var data in new[] { text, truncated, brokenChunk, new byte[0], new byte[] { 1, 2, 3 } })
            {
                var original = (byte[])data.Clone();

                Assert.IsFalse(WavVolume.Apply(data, 0.5));

                CollectionAssert.AreEqual(original, data);
            }

            Assert.IsFalse(WavVolume.Apply(null, 0.5));
            Assert.IsFalse(WavVolume.Apply(Wav(Chunk("fmt ", Format(1, 16)), Chunk("data", Pcm16(1000))), double.NaN));
        }

        [TestMethod]
        public void BundledSounds_AreUnderstood()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "YAPA", "Resources")))
            {
                directory = directory.Parent;
            }

            if (directory == null)
            {
                Assert.Inconclusive("The repository's YAPA\\Resources folder was not found");
            }

            foreach (var name in new[] { "ding.wav", "tick.wav" })
            {
                var wav = File.ReadAllBytes(Path.Combine(directory.FullName, "YAPA", "Resources", name));
                var loudestBefore = Loudest(wav);

                Assert.IsTrue(WavVolume.Apply(wav, 0.3), name);

                Assert.IsTrue(loudestBefore > 1000, $"{name} has sound in it");
                Assert.AreEqual(loudestBefore * 0.3, Loudest(wav), 1.0, name);
            }
        }

        private static int Loudest(byte[] wav)
        {
            //both bundled files are 16 bit with a 44 byte header
            var loudest = 0;
            for (var i = 44; i + 1 < wav.Length; i += 2)
            {
                loudest = Math.Max(loudest, Math.Abs((int)BitConverter.ToInt16(wav, i)));
            }
            return loudest;
        }
    }
}

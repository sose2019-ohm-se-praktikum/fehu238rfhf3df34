using System;
using System.IO;

namespace TestConsole
{
  internal class WaveSound : AudioFile
  {
    public WaveSound(string path) : base(path) { }

    private bool _little_endian;
    protected int dataChunkStart, dataChunkEnd, step;

    protected override double[] DecodeSamples(byte[] buffer)
    {
      _little_endian = buffer[3] == 0x46;
      if ((Read(buffer, 0, 4, false) == 0x52494658 | Read(buffer, 0, 4, false) == 0x52494646) & Read(buffer, 8, 4, false) == 0x57415645 & Read(buffer, 12, 4, false) == 0x666d7420 & Read(buffer, 20, 2) == 1) {
        dataChunkStart = 28 + Read(buffer, 16, 4);
        dataChunkEnd = Read(buffer, dataChunkStart - 4, 4) + dataChunkStart;
        step = (Read(buffer, 34, 2) + 7) / 8;
        double[] samples = new double[(dataChunkEnd - dataChunkStart) / step];
        for (int i = dataChunkStart, n = 0; i < dataChunkEnd; i += step)
          samples[n++] = (Read(buffer, i, step) << ((4 - step) * 8)) / (double)int.MaxValue;
        return samples;
      }
      else
        throw new InvalidDataException("Die angegebene Datei ist nicht im RIFF/X-WAVE-PCM-Format.");
    }

    protected override byte[] EncodeSamples(double[] samples, byte[] buffer)
    {
      for (int i = dataChunkStart, n = 0; i < dataChunkEnd; i += step) {
        int value = Convert.ToInt32(samples[n++] * int.MaxValue) >> ((4 - step) * 8);
        for (int k = 0; k < step; k++) {
          buffer[i + (_little_endian ? k : step - 1 - k)] = unchecked((byte)value);
          value >>= 8;
        }
      }
      return buffer;
    }

    private int Read(byte[] buffer, int start, int length, bool? little_endian = null)
    {
      bool le = little_endian ?? _little_endian;
      int value = 0;
      if (length > 4 | length < 1)
        throw new ArgumentException("Es können nur 1 bis 4 bytes in einen int gelesen werden.", "length");
      if (le)
        start += length - 1;
      for (int i = 0; i < length; i++)
        value = (value << 8) | buffer[start + (le ? -i : i)];
      return value;
    }
  }
}
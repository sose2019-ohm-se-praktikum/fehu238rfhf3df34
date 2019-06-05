using System;
using System.Collections.Generic;

namespace MFE
{
  internal class WaveSound : AudioFile
  {
    public WaveSound(string path) : base(path) { }

    private bool _little_endian;
    private int step;
    private long dataChunkStart;

    protected override double[] DecodeSamples(byte[] buffer)
    {
      _little_endian = buffer[3] == 0x46;
      if ((Read(buffer, 0, 4, false) == 0x52494658 | Read(buffer, 0, 4, false) == 0x52494646) & Read(buffer, 8, 4, false) == 0x57415645 & Read(buffer, 12, 4, false) == 0x666d7420 & Read(buffer, 20, 2) == 1) {
        dataChunkStart = 28 + Read(buffer, 16, 4);
        long dataChunkEnd = Read(buffer, dataChunkStart - 4, 4) + dataChunkStart;
        step = (Read(buffer, 34, 2) + 7) / 8;
        double[] samples = new double[(dataChunkEnd - dataChunkStart) / step];
        for (long i = dataChunkStart, n = 0; i < dataChunkEnd; i += step)
          samples[n++] = (Read(buffer, i, step) << ((4 - step) * 8)) / (double)int.MaxValue;
        return samples;
      }
      else
        throw new ArgumentException("The buffer does not contain a valid RIFF/RIFX wavesound in PCM format.", "buffer");
    }

    protected override byte[] EncodeSamples(IEnumerable<double> samples, byte[] buffer)
    {
      long i = dataChunkStart;
      byte[] output = new byte[buffer.LongLength];
      buffer.CopyTo(output, 0L);
      foreach (double sample in samples) {
        int value = Convert.ToInt32(sample * int.MaxValue) >> ((4 - step) * 8);
        try {
          for (int k = 0; k < step; k++) {
            output[i + (_little_endian ? k : step - 1 - k)] = unchecked((byte)value);
            value >>= 8;
          }
        }
        catch {
          throw new ArgumentException("The buffer does not contain the original file or was corrupted.", "buffer");
        }
        i += step;
      }
      return output;
    }

    private int Read(byte[] buffer, long start, int length, bool? little_endian = null)
    {
      bool le = little_endian ?? _little_endian;
      int value = 0;
      if (length > 4 | length < 1)
        throw new ArgumentException("Parameter must be in range [1,4].", "length");
      if (le)
        start += length - 1;
      for (int i = 0; i < length; i++)
        value = (value << 8) | buffer[start + (le ? -i : i)];
      return value;
    }
  }
}
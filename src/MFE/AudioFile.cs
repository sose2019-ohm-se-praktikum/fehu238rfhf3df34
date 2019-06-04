using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace TestConsole
{
  internal abstract class AudioFile<IdentifierType>
  {
    /// <summary>
    /// Defines a formula that returns the relative weight of a sample regarding average calculation.
    /// </summary>
    /// <param name="ratio">absolute ratio |sample / average|</param>
    /// <returns>the relative weight of the sample</returns>
    public delegate double WeighSampleFormula(double ratio);

    /// <summary>
    /// Defines a formula that returns the adjusted sample.
    /// </summary>
    /// <param name="sample">the sample to be adjusted [-1,1]</param>
    /// <param name="targetAverage">the weighted average to reach [0,1]</param>
    /// <param name="currentAverage">the weighted average of the current file [0,1]</param>
    /// <returns>the new sample [-1,1]</returns>
    public delegate double AdjustSampleFormula(double sample, double targetAverage, double currentAverage);

    /// <summary>
    /// Defines a formula that returns the relative weight of a sample regarding average calculation. The default is 1 for all ratios.
    /// </summary>
    public static WeighSampleFormula WeightFormula {
      get => wsf;
      set => wsf = value ?? wsf;
    }

    /// <summary>
    /// Defines a formula that returns the adjusted sample. The default is sample * targetAverage / currentAverage.
    /// </summary>
    public static AdjustSampleFormula AdjustmentFormula {
      get => asf;
      set => asf = value ?? asf;
    }

    private static WeighSampleFormula wsf = ratio => 1d;
    private static AdjustSampleFormula asf = (sample, target, current) => sample * target / current;

    /// <summary>
    /// Creates an AudioFile from a buffer.
    /// </summary>
    /// <param name="buffer">the file to modify</param>
    /// <param name="token"></param>
    /// <returns>the created AudioFile on success, null otherwise</returns>
    public static async Task<AudioFile<IdentifierType>> FromBuffer(byte[] buffer, CancellationToken token = default(CancellationToken))
    {
      return await Task.Run(() => {
        try {
          return new WaveSound<IdentifierType>(buffer);
        }
        catch {
          return null;
        }
      }, token);
    }

    /// <summary>
    /// optional - an object that can be used to identify the AudioFile
    /// </summary>
    public IdentifierType Identifier { get; set; }

    /// <summary>
    /// the weighted average as calculated using AudioFile.WeightFormula
    /// </summary>
    public double WeightedAverage { get; }

    /// <summary>
    /// the highest amplitude of all samples [0,1]
    /// </summary>
    public double Highest { get; }

    protected AudioFile(byte[] buffer)
    {
      Identifier = default(IdentifierType);
      double[] samples = DecodeSamples(buffer);
      Highest = samples.Max(sample => Math.Abs(sample));
      double average = samples.Average(sample => Math.Abs(sample)), dividend = 0d, divisor = 0d;
      foreach (double sample in samples) {
        double weight = WeightFormula(sample / average);
        dividend += sample * weight;
        divisor += weight;
      }
      WeightedAverage = dividend / divisor;
    }

    /// <summary>
    /// The samples are read from the buffer, adjusted and written back to it.
    /// </summary>
    /// <param name="targetAverage">the average to reach</param>
    /// <param name="buffer">the buffer that contains the file to be modified - This will contain the new bytes on success.</param>
    /// <param name="token"></param>
    /// <returns>true on success, false on error</returns>
    public async Task<bool> AdjustSamples(double targetAverage, byte[] buffer, CancellationToken token = default(CancellationToken))
    {
      return await Task.Run(() => {
        if (WouldFit(targetAverage)) {
          try {
            EncodeSamples(DecodeSamples(buffer).Select(sample => AdjustmentFormula(sample, targetAverage, WeightedAverage)), buffer).CopyTo(buffer, 0L);
            return true;
          }
          catch { }
        }
        return false;
      }, token);
    }

    /// <summary>
    /// Checks whether reaching this target average does not cause overmodulation.
    /// </summary>
    /// <param name="targetAverage">the average to reach</param>
    /// <returns>true if the samples fit, false on overmodulation</returns>
    public bool WouldFit(double targetAverage) => 1d >= AdjustmentFormula(Highest, targetAverage, WeightedAverage);

    protected abstract double[] DecodeSamples(byte[] buffer);
    protected abstract byte[] EncodeSamples(IEnumerable<double> samples, byte[] buffer);
  }
}
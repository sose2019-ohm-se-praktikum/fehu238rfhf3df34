using System;
using System.IO;
using System.Linq;

namespace TestConsole
{
  internal abstract class AudioFile
  {
    public delegate double WeighSampleFormula(double ratio);
    public delegate double AdjustSampleFormula(double sample, double targetAverage, double currentAverage);

    public static WeighSampleFormula weighSampleFormula;
    public static AdjustSampleFormula adjustSampleFormula;

    public string FullName { get; private set; }
    public double WeightedAverage { get; private set; }
    public double Highest { get; private set; }

    public AudioFile(string path)
    {
      if (weighSampleFormula == null | adjustSampleFormula == null)
        throw new InvalidOperationException("Die statischen Variablen weighSampleFormula & adjustSampleFormula müssen definiert sein.");
      FullName = path;
      double[] samples = DecodeSamples(File.ReadAllBytes(FullName));
      Highest = Math.Max(samples.Max(), -samples.Min());
      double average = samples.Average(), dividend = 0d, divisor = 0d;
      foreach (double sample in samples) {
        double weight = weighSampleFormula(sample / average);
        dividend += sample * weight;
        divisor += weight;
      }
      WeightedAverage = dividend / divisor;
    }

    public void AdjustSamples(double targetAverage, string path)
    {
      byte[] buffer = File.ReadAllBytes(FullName);
      double[] samples = DecodeSamples(buffer);
      for (int i = 0; i < samples.Length; i++)
        samples[i] = adjustSampleFormula(samples[i], targetAverage, WeightedAverage);
      File.WriteAllBytes(path, EncodeSamples(samples, buffer));
    }

    public bool WouldFit(double targetAverage) => 1d >= adjustSampleFormula(Highest, targetAverage, WeightedAverage);

    protected abstract double[] DecodeSamples(byte[] buffer);
    protected abstract byte[] EncodeSamples(double[] samples, byte[] buffer);
  }
}
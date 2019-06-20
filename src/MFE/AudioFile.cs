using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MFE
{
    internal abstract class AudioFile : IDisposable
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
        public static WeighSampleFormula WeightFormula
        {
            get => wsf;
            set => wsf = value ?? wsf;
        }

        /// <summary>
        /// Defines a formula that returns the adjusted sample. The default is sample * targetAverage / currentAverage.
        /// </summary>
        public static AdjustSampleFormula AdjustmentFormula
        {
            get => asf;
            set => asf = value ?? asf;
        }

        private static WeighSampleFormula wsf = ratio => 1d;
        private static AdjustSampleFormula asf = (sample, target, current) => sample * target / current;

        /// <summary>
        /// Tries to create an AudioFile.
        /// </summary>
        /// <param name="path">absolute path to a valid file</param>
        /// <returns>an AudioFile on success, null on failure</returns>
        public static AudioFile OpenFile(string path)
        {
            try
            {
                return new WaveSound(path);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// the weighted average as calculated using AudioFile.WeightFormula
        /// </summary>
        public double WeightedAverage { get; }

        /// <summary>
        /// the highest amplitude of all samples [0,1]
        /// </summary>
        public double Highest { get; }

        private byte[] Buffer
        {
            get
            {
                byte[] buffer = new byte[file.Length];
                file.Position = 0L;
                file.Read(buffer, 0, (int)file.Length);
                return buffer;
            }
        }

        private FileStream file;
        private string path;

        protected AudioFile(string filepath)
        {
            path = Path.GetFullPath(filepath);
            file = new FileStream(path, FileMode.Open, FileAccess.ReadWrite);
            file.Lock(0L, file.Length);
            double[] samples = DecodeSamples(Buffer);
            Highest = samples.Max(sample => Math.Abs(sample));
            double average = samples.Average(sample => Math.Abs(sample)), dividend = 0d, divisor = 0d;
            foreach (double sample in samples)
            {
                double weight = WeightFormula(sample / average);
                dividend += sample * weight;
                divisor += weight;
            }
            WeightedAverage = dividend / divisor;
        }

        ~AudioFile()
        {
            try
            {
                Dispose();
            }
            catch (Exception)
            {
            }
        }

        public void Dispose()
        {
            if (file != null)
            {
                file.Unlock(0L, file.Length);
                file.Dispose();
            }
        }

        /// <summary>
        /// The samples are read from the buffer, adjusted and written back to it.
        /// </summary>
        /// <param name="targetAverage">the average to reach</param>
        /// <param name="targetPath">the file path to write the new file to - null to write to the original</param>
        public void AdjustSamples(double targetAverage, string targetPath = null)
        {
            if (WouldFit(targetAverage))
            {
                byte[] buffer = Buffer;
                buffer = EncodeSamples(DecodeSamples(buffer).Select(sample => AdjustmentFormula(sample, targetAverage, WeightedAverage)), buffer);
                if (targetPath == null || PathComparer.Equals(path, targetPath))
                {
                    file.Position = 0L;
                    file.Write(buffer, 0, buffer.Length);
                }
                else
                    File.WriteAllBytes(targetPath, buffer);
            }
            else
                throw new ArgumentException("With the given target average overmodulation would occur.", "targetAverage");
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
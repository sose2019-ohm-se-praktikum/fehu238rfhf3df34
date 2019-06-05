using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MFE
{
  internal static class AudioFileManager
  {
    private static Dictionary<string, AudioFile> files = new Dictionary<string, AudioFile>(new PathComparer());

    /// <summary>
    /// Opens the specified file.
    /// </summary>
    /// <param name="path">the path to the file</param>
    public static void OpenFile(string path)
    {
      try {
        files[path].Dispose();
      }
      catch { }
      files[path] = AudioFile.OpenFile(path);
    }

    /// <summary>
    /// Closes the specified file.
    /// </summary>
    /// <param name="path">the path to the file</param>
    public static void CloseFile(string path)
    {
      files[path].Dispose();
      files.Remove(path);
    }

    /// <summary>
    /// Closes all open files.
    /// </summary>
    public static void CloseAll()
    {
      foreach (AudioFile file in files.Values)
        file.Dispose();
      files.Clear();
      files = null;
    }

    /// <summary>
    /// Checks all open files for overmodulation.
    /// </summary>
    /// <param name="progress">the object to notify of any progress made</param>
    /// <param name="factor">the factor to multiply the average volume with</param>
    /// <param name="ReferenceFiles">the files from which the average volume will be used to test all open files - if null or empty, then all files will be tested independently</param>
    /// <returns>true if all fits, false if at least one file overmodulates</returns>
    public static Task<bool> CheckForOvermodulation(IProgress<ProgressInfo> progress, double factor, IEnumerable<string> ReferenceFiles)
    {
      return Task.Run(() => {
        ProgressInfo info = new ProgressInfo { Progression = 0f, FailedFiles = new LinkedList<Tuple<string, Exception>>() };
        progress.Report(info);
        bool result = true;
        float total = files.Count, current = 0f;
        if (ReferenceFiles == null || ReferenceFiles.Count() < 1) {
          foreach (KeyValuePair<string, AudioFile> pair in files) {
            result &= pair.Value.WouldFit(factor * pair.Value.WeightedAverage);
            info.Progression = ++current / total;
            progress.Report(info);
          }
        }
        else {
          double target = files.Where(pair => ReferenceFiles.Contains(pair.Key, new PathComparer())).Average(pair => pair.Value.WeightedAverage) * factor;
          foreach (KeyValuePair<string, AudioFile> pair in files) {
            result &= pair.Value.WouldFit(target);
            info.Progression = ++current / total;
            progress.Report(info);
          }
        }
        return result;
      });
    }

    /// <summary>
    /// Adjusts all open files and writes them back to disk.
    /// </summary>
    /// <param name="progress">the object to notify of any progress made and errors encountered</param>
    /// <param name="factor">the factor to multiply the average volume with</param>
    /// <param name="ReferenceFiles">the files from which the average volume will be used to adjust all open files - if null or empty, then all files will be adjusted independently by the factor</param>
    /// <param name="NewPaths">the function that returns the path to save the file from the given path at</param>
    /// <returns></returns>
    public static Task AdjustFiles(IProgress<ProgressInfo> progress, double factor, IEnumerable<string> ReferenceFiles, Func<string, string> NewPaths)
    {
      return Task.Run(() => {
        ProgressInfo info = new ProgressInfo { Progression = 0f, FailedFiles = new LinkedList<Tuple<string, Exception>>() };
        progress.Report(info);
        float total = files.Count, current = 0f;
        if (ReferenceFiles == null || ReferenceFiles.Count() < 1) {
          foreach (KeyValuePair<string, AudioFile> pair in files) {
            try {
              pair.Value.AdjustSamples(factor * pair.Value.WeightedAverage, NewPaths(pair.Key));
            }
            catch (Exception exception) {
              info.FailedFiles.AddLast(new Tuple<string, Exception>(pair.Key, exception));
            }
            info.Progression = ++current / total;
            progress.Report(info);
          }
        }
        else {
          double target = files.Where(pair => ReferenceFiles.Contains(pair.Key, new PathComparer())).Average(pair => pair.Value.WeightedAverage) * factor;
          foreach (KeyValuePair<string, AudioFile> pair in files) {
            try {
              pair.Value.AdjustSamples(target, NewPaths(pair.Key));
            }
            catch (Exception exception) {
              info.FailedFiles.AddLast(new Tuple<string, Exception>(pair.Key, exception));
            }
            info.Progression = ++current / total;
            progress.Report(info);
          }
        }
      });
    }
  }
}
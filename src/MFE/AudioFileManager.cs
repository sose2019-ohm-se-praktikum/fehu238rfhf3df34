using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MFE
{
  public static class AudioFileManager
  {
    private static Dictionary<string, AudioFile> files = new Dictionary<string, AudioFile>(new PathComparer());

    /// <summary>
    /// Opens the specified file.
    /// </summary>
    /// <param name="path">the path to the file</param>
    /// <returns>true on success, false on failure</returns>
    public static bool OpenFile(string path)
    {
      try {
        files[path].Dispose();
      }
      catch { }
      AudioFile file = AudioFile.OpenFile(path);
      if (file == null)
        return false;
      else {
        files[path] = file;
        return true;
      }
    }

    /// <summary>
    /// Closes the specified file.
    /// </summary>
    /// <param name="path">the path to the file</param>
    public static void CloseFile(string path)
    {
      try {
        files[path].Dispose();
      }
      catch { }
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
    }

    /// <summary>
    /// Checks files for overmodulation.
    /// </summary>
    /// <param name="progress">the action to perform when progress is made (parameter: progression [0,1])</param>
    /// <param name="factor">the factor to multiply the average volume with</param>
    /// <param name="SelectedFiles">the files to adjust</param>
    /// <param name="ReferenceFiles">the files from which the average volume will be used to test all open files - if null or empty, then all files will be tested independently</param>
    /// <returns>an enumeration of all overmodulating files; an empty one if all fits</returns>
    public static Task<LinkedList<string>> CheckForOvermodulation(Action<float> progress, double factor, IEnumerable<string> SelectedFiles, IEnumerable<string> ReferenceFiles)
    {
      return Task.Run(() => {
        LinkedList<string> list = new LinkedList<string>();
        float total = SelectedFiles.Count(), current = 0f;
        if (ReferenceFiles == null || ReferenceFiles.Count() < 1) {
          foreach (string path in SelectedFiles) {
            if (!files[path].WouldFit(factor * files[path].WeightedAverage))
              list.AddLast(path);
            progress(++current / total);
          }
        }
        else {
          double target = files.Where(pair => ReferenceFiles.Contains(pair.Key, new PathComparer())).Average(pair => pair.Value.WeightedAverage) * factor;
          foreach (string path in SelectedFiles) {
            if (!files[path].WouldFit(target))
              list.AddLast(path);
            progress(++current / total);
          }
        }
        return list;
      });
    }

    /// <summary>
    /// Adjusts files and writes them back to disk.
    /// </summary>
    /// <param name="progress">the action to perform when progress is made (parameter: progression [0,1])</param>
    /// <param name="factor">the factor to multiply the average volume with</param>
    /// <param name="SelectedFiles">the files to adjust</param>
    /// <param name="ReferenceFiles">the files from which the average volume will be used to adjust all open files - if null or empty, then all files will be adjusted independently by the factor</param>
    /// <param name="NewPaths">the function that returns the path to save the file from the given path at</param>
    /// <param name="fileSucceed">the action to perform when a file is adjusted successfully (parameter: path to the file in question)</param>
    /// <param name="fileFailed">the action to perform when a file fails to be adjusted (parameters: path to the file in question, exception that marks the failure)</param>
    public static Task AdjustFiles(Action<float> progress, double factor, IEnumerable<string> SelectedFiles, IEnumerable<string> ReferenceFiles, Func<string, string> NewPaths, Action<string> fileSucceed, Action<string, Exception> fileFailed)
    {
      return Task.Run(() => {
        float total = SelectedFiles.Count(), current = 0f;
        if (ReferenceFiles == null || ReferenceFiles.Count() < 1) {
          foreach (string path in SelectedFiles) {
            try {
              files[path].AdjustSamples(factor * files[path].WeightedAverage, NewPaths(path));
              fileSucceed(path);
            }
            catch (Exception exception) {
              fileFailed(path, exception);
            }
            progress(++current / total);
          }
        }
        else {
          double target = files.Where(pair => ReferenceFiles.Contains(pair.Key, new PathComparer())).Average(pair => pair.Value.WeightedAverage) * factor;
          foreach (string path in SelectedFiles) {
            try {
              files[path].AdjustSamples(target, NewPaths(path));
              fileSucceed(path);
            }
            catch (Exception exception) {
              fileFailed(path, exception);
            }
            progress(++current / total);
          }
        }
      });
    }
  }
}
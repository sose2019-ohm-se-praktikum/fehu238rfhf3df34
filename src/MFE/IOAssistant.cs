using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace TestConsole
{
  internal enum IOResult
  {
    success = 0,
    unknown_error = 1,
    invalid_path = 2,
    io_error = 3,
    permission_missing = 4
  }

  internal static class IOAssistant
  {
    /// <summary>
    /// Reads the file specified by "path" into "content". No exceptions are thrown.
    /// </summary>
    /// <param name="path">This must be the path to an existing file. It may be absolute or relative to the current working directory. Wildcards and environment variables are not supported.</param>
    /// <param name="content">If IOResult.success is returned, this parameter contains the bytes of the file, otherwise it is null.</param>
    /// <returns>IOResult indicating success or a kind of error</returns>
    public static IOResult ReadFile(string path, out byte[] content)
    {
      byte[] buffer = null;
      IOResult result = AccessFile(path, () => buffer = File.ReadAllBytes(path));
      content = buffer;
      return result;
    }

    /// <summary>
    /// Writes the bytes specified by "content" to the file at "path". No exceptions are thrown.
    /// </summary>
    /// <param name="path">This must be a valid path to a file that will be created or overwritten. It may be absolute or relative to the current working directory. Wildcards and environment variables are not supported.</param>
    /// <param name="content">This parameter contains the bytes to be written to the file.</param>
    /// <returns>IOResult indicating success or a kind of error</returns>
    public static IOResult WriteFile(string path, byte[] content) => AccessFile(path, () => File.WriteAllBytes(path, content));

    /// <summary>
    /// Reads all files matching the given path.
    /// </summary>
    /// <param name="path">a valid file path that may contain wildcards and environment variables</param>
    /// <param name="token"></param>
    /// <returns>a LinkedList of the contents of each file that was found and could be read</returns>
    public static async Task<LinkedList<byte[]>> ReadFiles(string path, CancellationToken token = default(CancellationToken))
    {
      return await Task.Run(() => {
        path = Environment.ExpandEnvironmentVariables(path);
        LinkedList<byte[]> files = new LinkedList<byte[]>();
        try {
          foreach (string file in Directory.GetFiles(Path.GetDirectoryName(path), Path.GetFileName(path))) {
            if (ReadFile(file, out byte[] content) == IOResult.success)
              files.AddLast(content);
          }
        }
        catch { }
        return files;
      }, token);
    }

    private static IOResult AccessFile(string path, Action function)
    {
      if (Directory.Exists(path))
        return IOResult.invalid_path;
      try {
        function();
        return IOResult.success;
      }
      catch (ArgumentException) {
        return IOResult.invalid_path;
      }
      catch (PathTooLongException) {
        return IOResult.invalid_path;
      }
      catch (DirectoryNotFoundException) {
        return IOResult.invalid_path;
      }
      catch (FileNotFoundException) {
        return IOResult.invalid_path;
      }
      catch (IOException) {
        return IOResult.io_error;
      }
      catch (UnauthorizedAccessException) {
        return IOResult.permission_missing;
      }
      catch (NotSupportedException) {
        return IOResult.invalid_path;
      }
      catch (SecurityException) {
        return IOResult.permission_missing;
      }
      catch {
        return IOResult.unknown_error;
      }
    }
  }
}
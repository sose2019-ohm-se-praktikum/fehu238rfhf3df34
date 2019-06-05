using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

namespace MFE
{
  class AudioFileManager
  {
    private static Dictionary<string, AudioFile> files = new Dictionary<string, AudioFile>();

    public static void OpenFile(string path)
    {
      if()
    }

    public static void RemoveFile(string path)
    {
      
    }
  }

  class PathComparer : IEqualityComparer<string>
  {
    bool IEqualityComparer<string>.Equals(string x, string y)
    {
      x = Path.GetFullPath(x);
      y = Path.GetFullPath(y);
      return string.Equals(x,y, StringComparison.OrdinalIgnoreCase);
    }

    int IEqualityComparer<string>.GetHashCode(string obj)
    {
     return Path.GetFullPath(obj).GetHashCode();
    }
  }
}
using System;
using System.Collections.Generic;
using System.IO;

namespace MFE
{
  internal class PathComparer : IEqualityComparer<string>
  {
    public static bool Equals(string x, string y)
    {
      x = Path.GetFullPath(x);
      y = Path.GetFullPath(y);
      return string.Equals(x, y, StringComparison.OrdinalIgnoreCase);
    }

    bool IEqualityComparer<string>.Equals(string x, string y) => Equals(x, y);

    int IEqualityComparer<string>.GetHashCode(string obj) => Path.GetFullPath(obj.ToLower()).GetHashCode();
  }
}
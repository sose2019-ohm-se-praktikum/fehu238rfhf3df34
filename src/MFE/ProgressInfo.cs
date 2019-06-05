using System;
using System.Collections.Generic;

namespace MFE
{
  internal struct ProgressInfo
  {
    public float Progression { get; set; }
    public LinkedList<Tuple<string, Exception>> FailedFiles { get; set; }
  }
}
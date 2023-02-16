using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CompatTests.Util
{
  public class Food4RhinoSource : IEnumerable
  {
    public IEnumerator GetEnumerator()
    {
      yield break;
    }
  }
}

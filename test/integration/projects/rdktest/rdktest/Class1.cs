using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino;

namespace rdktest
{
  public class Class1 : Rhino.PlugIns.RenderPlugIn
  {
    protected override Rhino.Commands.Result Render(RhinoDoc doc, Rhino.Commands.RunMode mode, RenderOptions options)
    {
      return Rhino.Commands.Result.Failure;
    }
  }
}

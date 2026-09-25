using System;
using Grasshopper.Kernel;

namespace CompatibleComponents
{
  // A Rhino 8 component that works in Rhino 9: it gets checked, but should load with no message.
  public class CompatibleComponent : GH_Component
  {
    public CompatibleComponent()
      : base("Compatible Test", "Compat", "Works in Rhino 9", "Params", "Util") { }

    public override Guid ComponentGuid => new Guid("6f0b1c52-3c1e-4d0a-9a55-5a1f0d7e2b06");

    protected override void RegisterInputParams(GH_InputParamManager pManager) { }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager) { }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      Rhino.RhinoApp.WriteLine("CompatibleComponent ran");
    }
  }
}

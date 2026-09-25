using System;
using Grasshopper.Kernel;

namespace NetFrameworkComponents
{
  // A Rhino 8 component that uses AppDomains, which .NET Core doesn't support.
  // It still loads, so it should show up under Params > Util.
  public class NetFrameworkComponent : GH_Component
  {
    public NetFrameworkComponent()
      : base("NetFramework Test", "NetFx", "Uses AppDomain.CreateDomain", "Params", "Util") { }

    public override Guid ComponentGuid => new Guid("6f0b1c52-3c1e-4d0a-9a55-5a1f0d7e2b05");

    protected override void RegisterInputParams(GH_InputParamManager pManager) { }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager) { }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      var domain = AppDomain.CreateDomain("NetFrameworkComponents");
      AppDomain.Unload(domain);
    }
  }
}

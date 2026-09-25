using System.Reflection;

[assembly: AssemblyVersion("8.0.0.0")]

namespace Grasshopper
{
  public sealed class Instances
  {
    // not in Rhino 9, so anything calling it fails the compatibility check
    public static void RemovedInRhino9() { }
  }
}

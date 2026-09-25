using System.Reflection;

[assembly: AssemblyVersion("8.0.0.0")]

namespace Rhino
{
  public static class RhinoApp
  {
    public static void WriteLine(string message) { }

    // not in Rhino 9, so anything calling it fails the compatibility check
    public static void RemovedInRhino9() { }
  }
}

namespace Rhino.PlugIns
{
  public abstract class PlugIn
  {
    protected PlugIn() { }
  }

  // Same names and values as RhinoCommon, which Rhino reads the developer's contact details from
  public enum DescriptionType
  {
    Organization,
    Address,
    Country,
    Phone,
    WebSite,
    Email,
    UpdateUrl,
    Fax,
    Icon
  }

  [System.AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple = true)]
  public sealed class PlugInDescriptionAttribute : System.Attribute
  {
    public PlugInDescriptionAttribute(DescriptionType descriptionType, string value) { }
  }
}

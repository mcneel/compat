using System.Runtime.InteropServices;
using Rhino.PlugIns;

[assembly: Guid("6f0b1c52-3c1e-4d0a-9a55-5a1f0d7e2b01")]

// Every contact detail, to fill the load error dialog's vendor section
[assembly: PlugInDescription(DescriptionType.Organization, "Example Plug-in Co.")]
[assembly: PlugInDescription(DescriptionType.Address, "123 Example Street\nSuite 400\nSeattle, WA 98101")]
[assembly: PlugInDescription(DescriptionType.Country, "United States")]
[assembly: PlugInDescription(DescriptionType.Phone, "+1 206 555 0100")]
[assembly: PlugInDescription(DescriptionType.Fax, "+1 206 555 0101")]
[assembly: PlugInDescription(DescriptionType.Email, "support@example.com")]
[assembly: PlugInDescription(DescriptionType.WebSite, "https://www.example.com/rhino-api-plugin")]
[assembly: PlugInDescription(DescriptionType.UpdateUrl, "https://www.example.com/rhino-api-plugin/download")]

namespace RhinoApiPlugin
{
  // Built against a Rhino 8 RhinoCommon, and calls a method Rhino 9 doesn't have.
  public class RhinoApiPlugin : Rhino.PlugIns.PlugIn
  {
    public RhinoApiPlugin()
    {
      Rhino.RhinoApp.RemovedInRhino9();
    }
  }
}

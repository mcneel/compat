using System.Runtime.InteropServices;
using Rhino;
using Rhino.Commands;
using Rhino.PlugIns;

[assembly: Guid("6f0b1c52-3c1e-4d0a-9a55-5a1f0d7e2b07")]

// Every contact detail, to fill the plug-in properties' vendor section
[assembly: PlugInDescription(DescriptionType.Organization, "Example Compatible Software")]
[assembly: PlugInDescription(DescriptionType.Address, "42 Harbour Road\nAuckland 1010")]
[assembly: PlugInDescription(DescriptionType.Country, "New Zealand")]
[assembly: PlugInDescription(DescriptionType.Phone, "+64 9 555 0100")]
[assembly: PlugInDescription(DescriptionType.Fax, "+64 9 555 0101")]
[assembly: PlugInDescription(DescriptionType.Email, "hello@example.com")]
[assembly: PlugInDescription(DescriptionType.WebSite, "https://www.example.com/compatible-plugin")]
[assembly: PlugInDescription(DescriptionType.UpdateUrl, "https://www.example.com/compatible-plugin/download")]

namespace CompatiblePlugin
{
  // A Rhino 8 plug-in that works in Rhino 9: it gets checked, but should load with no message.
  public class CompatiblePlugin : Rhino.PlugIns.PlugIn
  {
    public CompatiblePlugin() { }
  }

  public class CompatibleTestCommand : Command
  {
    public override string EnglishName => "CompatibleTest";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
      RhinoApp.WriteLine("CompatiblePlugin ran");
      return Result.Success;
    }
  }
}

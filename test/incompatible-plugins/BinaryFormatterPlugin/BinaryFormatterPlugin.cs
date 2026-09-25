using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using Rhino.PlugIns;

[assembly: Guid("6f0b1c52-3c1e-4d0a-9a55-5a1f0d7e2b03")]

// Every contact detail, to fill the load dialogs' vendor section
[assembly: PlugInDescription(DescriptionType.Organization, "Example Serialization Ltd.")]
[assembly: PlugInDescription(DescriptionType.Address, "Hauptstraße 12\n10115 Berlin")]
[assembly: PlugInDescription(DescriptionType.Country, "Germany")]
[assembly: PlugInDescription(DescriptionType.Phone, "+49 30 555 0100")]
[assembly: PlugInDescription(DescriptionType.Fax, "+49 30 555 0101")]
[assembly: PlugInDescription(DescriptionType.Email, "support@example.net")]
[assembly: PlugInDescription(DescriptionType.WebSite, "https://www.example.net/binaryformatter-plugin")]
[assembly: PlugInDescription(DescriptionType.UpdateUrl, "https://www.example.net/binaryformatter-plugin/download")]

namespace BinaryFormatterPlugin
{
  // A Rhino 8 plug-in that uses BinaryFormatter. Rhino turns it on, but Rhino.Inside hosts may not.
  public class BinaryFormatterPlugin : Rhino.PlugIns.PlugIn
  {
    public BinaryFormatterPlugin() { }

    static void Serialize()
    {
      using (var stream = new MemoryStream())
        new BinaryFormatter().Serialize(stream, "hello");
    }
  }
}

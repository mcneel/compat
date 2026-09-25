using System;
using System.Runtime.InteropServices;
using Rhino.PlugIns;

[assembly: Guid("6f0b1c52-3c1e-4d0a-9a55-5a1f0d7e2b02")]

// Every contact detail; the sites have no scheme, as vendors often write them
[assembly: PlugInDescription(DescriptionType.Organization, "Example Legacy Tools")]
[assembly: PlugInDescription(DescriptionType.Address, "Unit 7, Old Mill Lane\nLeeds LS1 4AB")]
[assembly: PlugInDescription(DescriptionType.Country, "United Kingdom")]
[assembly: PlugInDescription(DescriptionType.Phone, "+44 113 496 0000")]
[assembly: PlugInDescription(DescriptionType.Fax, "+44 113 496 0001")]
[assembly: PlugInDescription(DescriptionType.Email, "help@example.org")]
[assembly: PlugInDescription(DescriptionType.WebSite, "www.example.org")]
[assembly: PlugInDescription(DescriptionType.UpdateUrl, "www.example.org/downloads")]

namespace NetFrameworkPlugin
{
  // A Rhino 8 plug-in that uses AppDomains, which .NET Core doesn't support.
  public class NetFrameworkPlugin : Rhino.PlugIns.PlugIn
  {
    public NetFrameworkPlugin() { }

    static void UseAppDomain()
    {
      var domain = AppDomain.CreateDomain("NetFrameworkPlugin");
      AppDomain.Unload(domain);
    }
  }
}

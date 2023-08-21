using Compat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CompatTests
{
  [TestFixture]
  public class HelperTests
  {

    [Test]
    public void GenerateSystemAssemblyList()
    {
      // only add .NET Core assemblies
      if (!Program.NetCore.RunningInNetCore)
      {
        Assert.Fail("This test should only be run in .NET Core");
      }

      var dir = Path.GetDirectoryName(typeof(object).Assembly.Location);
      foreach (var fileName in Directory.GetFiles(dir, "*.dll").OrderBy(Path.GetFileName))
      {
        try
        {
          var name = Path.GetFileNameWithoutExtension(fileName);
          var assemblyName = AssemblyName.GetAssemblyName(fileName);
          var tokenName = Program.GetPublicKeyTokenName(assemblyName.GetPublicKeyToken());
          Console.WriteLine($"    (\"{name}\", \"{tokenName}\"),");
        }
        catch
        {
          // not a .NET assembly, ignore
        }
      }
    }

    [Test]
    public void GenerateRhinoNetCoreAssemblyList()
    {
      // only add .NET Core assemblies

      // find the src4 directory
      var dir = AppContext.BaseDirectory;
      while (dir != null && Path.GetFileName(dir) != "src4")
      {
        dir = Path.GetDirectoryName(dir);
      }


      dir = Path.Combine(dir, "bin", "Debug", "netcore");
      if (!Directory.Exists(dir))
      {
        Assert.Fail("Could not find src4\\bin\\Debug\\netcore directory.  Build Rhino first!");
      }


      foreach (var fileName in Directory.GetFiles(dir, "*.dll").OrderBy(Path.GetFileName))
      {
        try
        {
          var name = Path.GetFileNameWithoutExtension(fileName);
          var assemblyName = AssemblyName.GetAssemblyName(fileName);
          var tokenName = Program.GetPublicKeyTokenName(assemblyName.GetPublicKeyToken());
          if (tokenName == null)
            continue;

          // ignore these
          if (name == "Compat" || name == "dotnetstart" || name == "RhinoCommon")
            continue;

          Console.WriteLine($"    (\"{name}\", \"{tokenName}\"),");
        }
        catch
        {
          // not a .NET assembly, ignore
        }
      }
    }
  }
}

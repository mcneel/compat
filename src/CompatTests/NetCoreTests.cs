using System.Reflection;

namespace CompatTests;

public class NetCoreTests : TestBase
{
  [Test]
  public void TestNetFxPluginWithExceptionApis()
  {
    var rhinoCommon = GetRhinoCommon("rhino_en-us_8.0.23045.12305");

    var result = RunCompatCheck(GetTestProject("NetFxPlugin"), new [] { rhinoCommon }, includeSystemAssemblies: true);

    Assert.That(result.ExitCode, Is.EqualTo(Compat.Program.ERROR_COMPAT));
    
    foreach (var entry in Compat.Program.NetCore.GetNetCoreExceptionApis())
    {
      var status = $"{GetStatusText(entry.status)} {entry.api} < {entry.assembly}";
      Assert.That(result.Output.Contains(status), $"Could not find API '{entry.api}' in the plugin");
    }
  }
}

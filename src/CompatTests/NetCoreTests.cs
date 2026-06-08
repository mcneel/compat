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

  [Test]
  public void TestNetFxPluginWithNet10Check()
  {
    var rhinoCommon = GetRhinoCommon("rhino_en-us_8.0.23045.12305");

    // BinaryFormatter resolves fine, so it's only flagged when --check-net10 is enabled
    var result = RunCompatCheck(GetTestProject("NetFxPlugin"), new[] { rhinoCommon }, includeSystemAssemblies: true, checkNet10: true);

    Assert.That(result.ExitCode, Is.EqualTo(Compat.Program.ERROR_COMPAT));

    foreach (var type in Compat.Program.NetCore.Net10ExceptionTypes.Keys)
    {
      Assert.That(result.Output.Contains($"{GetStatusText(Compat.Program.ResolutionStatus.Failure)} ") && result.Output.Contains(type),
        $"Expected '{type}' to be flagged as a failure with --check-net10");
    }
  }

  [Test]
  public void TestNetFxPluginWithoutNet10CheckDoesNotFlagBinaryFormatter()
  {
    var rhinoCommon = GetRhinoCommon("rhino_en-us_8.0.23045.12305");

    // Without --check-net10, BinaryFormatter resolves successfully and must not be reported as a failure
    var result = RunCompatCheck(GetTestProject("NetFxPlugin"), new[] { rhinoCommon }, includeSystemAssemblies: true);

    var binaryFormatter = "System.Runtime.Serialization.Formatters.Binary.BinaryFormatter";
    foreach (var line in result.Output.Split('\n'))
    {
      if (line.Contains(binaryFormatter))
        Assert.That(line.Contains(GetStatusText(Compat.Program.ResolutionStatus.Failure)), Is.False,
          $"BinaryFormatter should not be flagged as a failure without --check-net10: {line.Trim()}");
    }
  }
}

using CompatTests.Util;

namespace CompatTests;

public class ThirdPartyTests : TestBase
{
  [Test]
  [TestCaseSource(typeof(YakSource))]
  //[TestCaseSource(typeof(DirectorySource), nameof(DirectorySource.Get), new object[] { "yak", true })]
  public Task TestYakPackage(IPackageSource package) => TestPackage(package);

  [Test]
  [TestCaseSource(typeof(Food4RhinoSource))]
  // [TestCaseSource(typeof(DirectorySource), nameof(DirectorySource.Get), new object[] { "f4r", true })]
  public Task TestF4RPackage(IPackageSource package) => TestPackage(package);

  async Task TestPackage(IPackageSource package)
  {
    var packagePath = await package.Download();

    var rhinoCommon = GetRhinoCommon("rhino_en-us_8.0.23045.12305");

    var result = RunCompatCheck(packagePath, new[] { rhinoCommon }, quiet: true, includeSystemAssemblies: true);

    if (result.ExitCode != 0)
    {
      var resultsPath = Path.Combine(AppContext.BaseDirectory, "results", OSName);
      if (!Directory.Exists(resultsPath))
        Directory.CreateDirectory(resultsPath);
      resultsPath = Path.Combine(resultsPath, package.Name + ".txt");
      File.WriteAllText(resultsPath, result.Output);

      Console.WriteLine(result.Output);
    }

    Assert.That(result.ExitCode, Is.Not.EqualTo(Compat.Program.ERROR_COMPAT));
    Warn.If(result.ExitCode, Is.EqualTo(Compat.Program.ERROR_WARNING));
  }

  //[Test]
  //[TestCase("Enscape", @"z:\Downloads\Enscape\Bin64")]
  //[TestCase("IRay", @"z:\Downloads\Clayoo_and_iRay\Rhino_IRAY_Plugin")]
  public Task TestSinglePackage(string name, string path) => 
    TestPackage(new DirectoryPackageSource(name, path));

}

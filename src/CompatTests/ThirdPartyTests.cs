using CompatTests.Util;
using System.Collections;

namespace CompatTests;

public class ThirdPartyTests : TestBase
{
  string ResultsPath => Path.Combine(AppContext.BaseDirectory, "results", OSName);

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
    var resultsPath = ResultsPath;
    var resultsFileName = Path.Combine(resultsPath, package.Name + ".txt");
    
    // clear out old results
    if (File.Exists(resultsFileName))
      File.Delete(resultsFileName);

    resultsFileName = Path.Combine(resultsPath, "warn", package.Name + ".txt");
    if (File.Exists(resultsFileName))
      File.Delete(resultsFileName);

    var packagePath = await package.Download();

    var rhinoCommon = GetRhinoCommon("rhino_en-us_8.0.23206.14395");

    var result = RunCompatCheck(packagePath, new[] { rhinoCommon }, quiet: true, includeSystemAssemblies: true);

    if (result.ExitCode != 0)
    { 
      if (result.ExitCode != Compat.Program.ERROR_COMPAT)
        resultsPath = Path.Combine(resultsPath, "warn");

      if (!Directory.Exists(resultsPath))
        Directory.CreateDirectory(resultsPath);

      resultsFileName = Path.Combine(resultsPath, package.Name + ".txt");
      File.WriteAllText(resultsFileName, result.Output);

      Console.WriteLine(result.Output);
    }

    Assert.That(result.ExitCode, Is.Not.EqualTo(Compat.Program.ERROR_COMPAT));
    Warn.If(result.ExitCode, Is.EqualTo(Compat.Program.ERROR_WARNING));
  }

  [Test]
  [TestCase(typeof(YakSource))]
  [TestCase(typeof(Food4RhinoSource))]
  public async Task DownloadPackages(Type type)
  {
    var source = (BaseSource)Activator.CreateInstance(type);
    await source.DownloadAll();
  }


  //[Test]
  //[TestCase("Enscape", @"z:\Downloads\Enscape\Bin64")]
  //[TestCase("IRay", @"z:\Downloads\Clayoo_and_iRay\Rhino_IRAY_Plugin")]
  public Task TestSinglePackage(string name, string path) => 
    TestPackage(new DirectoryPackageSource(name, path));

}

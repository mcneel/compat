using Compat;
using CompatTests.Util;
using System.Collections;
using System.IO;

namespace CompatTests;

public class ThirdPartyTests : TestBase
{
  string ResultsPath => Path.Combine(GetRootDir(), "results", OSName, Compat.Program.NetCore.RunningInNetCore ? "netcore" : "netfx");

  [Test]
  [TestCaseSource(typeof(YakSource))]
  //[TestCaseSource(typeof(DirectorySource), nameof(DirectorySource.Get), new object[] { "yak", true })]
  public Task TestYakPackage(IPackageSource package) => TestPackage(package, "yak");

  [Test]
  [TestCaseSource(typeof(Food4RhinoSource))]
  // [TestCaseSource(typeof(DirectorySource), nameof(DirectorySource.Get), new object[] { "f4r", true })]
  public Task TestF4RPackage(IPackageSource package) => TestPackage(package, "f4r");

  enum Mode
  {
    Pass,
    Fail,
    Warn,
    Not_Testable,
    Installer_Only
  }

  async Task TestPackage(IPackageSource package, string subdir)
  {
    var resultsPath = Path.Combine(ResultsPath, subdir);

    string GetResultFileName(Mode mode) => Path.Combine(resultsPath, mode.ToString().ToLower(), package.Name + ".txt");

    // clear out any old results
    foreach (var m in Enum.GetValues(typeof(Mode)).Cast<Mode>())
    {
      var fn = GetResultFileName(m);
      if (File.Exists(fn))
        File.Delete(fn);
    }

    var packagePath = await package.Download();

    var rhinoCommon = GetRhinoCommon("rhino_en-us_8.0.23206.14395");

    var result = RunCompatCheck(packagePath, new[] { rhinoCommon }, quiet: true, includeSystemAssemblies: true);

    var mode = result.ExitCode == 0 ? Mode.Pass
      : result.ExitCode == -1 ? Mode.Not_Testable
      : result.ExitCode == Compat.Program.ERROR_COMPAT ? Mode.Fail
      : Mode.Warn;

    if (mode == Mode.Not_Testable)
    {
      // check to see if there are only .exe's
      var onlyExes = Directory.GetFiles(packagePath, "*.*", SearchOption.AllDirectories).All(r => Path.GetExtension(r).ToLower() == ".exe");
      if (onlyExes)
        mode = Mode.Installer_Only;
    }

    var resultsFileName = GetResultFileName(mode);

    if (!Directory.Exists(Path.GetDirectoryName(resultsFileName)))
      Directory.CreateDirectory(Path.GetDirectoryName(resultsFileName));

    File.WriteAllText(resultsFileName, result.Output);

    Console.WriteLine(result.Output);

    Assert.That(result.ExitCode, Is.Not.EqualTo(Compat.Program.ERROR_COMPAT).Or.EqualTo(-1));
    Warn.If(result.ExitCode, Is.EqualTo(Compat.Program.ERROR_WARNING).Or.EqualTo(-1));
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
    TestPackage(new DirectoryPackageSource(name, path), "single");

}

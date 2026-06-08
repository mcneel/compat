using Compat;
using CompatTests.Util;
using System.Collections;
using System.IO;
using System.Text;

namespace CompatTests;

[TestFixture]
public class ThirdPartyTests : TestBase
{
  string ResultsPath => Path.Combine(GetRootDir(), "results", OSName, Compat.Program.NetCore.RunningInNetCore ? "netcore" : "netfx");

  [Test]
  [TestCaseSource(typeof(YakSource))]
  //[TestCaseSource(typeof(DirectorySource), nameof(DirectorySource.Get), new object[] { "yak", true })]
  public Task TestYakPackage(IPackageSource package) => TestPackage(package, "yak", topLevelOnly: true);

  [Test]
  [TestCaseSource(typeof(Food4RhinoSource))]
  // [TestCaseSource(typeof(DirectorySource), nameof(DirectorySource.Get), new object[] { "f4r", true })]
  public Task TestF4RPackage(IPackageSource package) => TestPackage(package, "f4r", topLevelOnly: false);

  enum Mode
  {
    Pass,
    Conditional_Pass,
    Fail,
    Warn,
    Not_Testable,
    Has_Installer,
    Grasshopper
  }

  async Task TestPackage(IPackageSource package, string subdir, string? rhinoCommonPath = null, bool topLevelOnly = true)
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

    var rhinoCommon = rhinoCommonPath ?? GetRhinoCommon("rhino_9.0.26153.12415");

    var referenceAssemblies = new[] { rhinoCommon };

    var result = RunCompatCheck(packagePath, referenceAssemblies, quiet: true, includeSystemAssemblies: true, checkNet10: RunningInNetCore, topLevelOnly: topLevelOnly);

    var output = new StringBuilder();

    // collect any API breaks against the Rhino reference assemblies so we can summarize them later
    var rhinoAssemblyNames = referenceAssemblies
      .Select(r => Path.GetFileNameWithoutExtension(r))
      .ToHashSet(StringComparer.OrdinalIgnoreCase);
    CollectApiBreaks(subdir, package.Name, result.Output, rhinoAssemblyNames);

    var mode = result.ExitCode == 0 ? Mode.Pass
      : result.ExitCode == -1 ? Mode.Not_Testable
      : result.ExitCode == Compat.Program.ERROR_COMPAT ? Mode.Fail
      : Mode.Warn;
      
    if (mode == Mode.Fail)
    {
      // if it only has BinaryFormatter failures, it's a conditional pass
      var hasBinaryFormatterFailures = result.Output.Split('\n').Any(line => line.Contains("System.Runtime.Serialization.Formatters.Binary.BinaryFormatter"));
      
      // ensure that the only failures are BinaryFormatter failures
      if (hasBinaryFormatterFailures && result.Output.Split('\n').All(line => line.Contains("System.Runtime.Serialization.Formatters.Binary.BinaryFormatter") || !line.Contains("✗ FAIL")))
        mode = Mode.Conditional_Pass;
    }

    if (mode == Mode.Not_Testable)
    {
      // does it have a plugin file? if so it was probably just not .NET based
      var hasPlugin = PluginExtensions.SelectMany(ext => Directory.GetFiles(packagePath, ext, SearchOption.AllDirectories)).Any();
      if (!hasPlugin)
      {
        // only grasshopper stuff?
        var hasGh = GrasshopperExtensions.SelectMany(ext => Directory.GetFiles(packagePath, ext, SearchOption.AllDirectories)).Any();
        if (hasGh)
          mode = Mode.Grasshopper;
        else
        {
          // check to see if there are only .exe's
          var hasExes = InstallerExtensions.SelectMany(ext => Directory.GetFiles(packagePath, ext, SearchOption.AllDirectories)).Any();
          if (hasExes)
            mode = Mode.Has_Installer;
        }
      }
    }

    var typesOfFilesAndCount = Directory.GetFiles(packagePath, "*.*", SearchOption.AllDirectories).Select(r => Path.GetExtension(r).ToLower()).GroupBy(r => r).Select(g => (Type: g.Key, Count: g.Count())).ToList();
    foreach (var t in typesOfFilesAndCount.OrderBy(t => t.Type))
      output.AppendLine($"Found file type: {t.Type}, Count: {t.Count}");

    var resultsFileName = GetResultFileName(mode);

    if (!Directory.Exists(Path.GetDirectoryName(resultsFileName)))
      Directory.CreateDirectory(Path.GetDirectoryName(resultsFileName));

    if (output.Length > 0)
    {
      output.AppendLine();
    }
    
    output.AppendLine(result.Output);
      
    File.WriteAllText(resultsFileName, output.ToString());

    Console.WriteLine(result.Output);

    Assert.That(result.ExitCode, Is.Not.EqualTo(Program.ERROR_COMPAT).Or.EqualTo(-1).Or.EqualTo(Program.ERROR_NOT_DOTNET));
    Warn.If(result.ExitCode, Is.EqualTo(Program.ERROR_WARNING).Or.EqualTo(-1));
  }



  // [TestCase("Enscape", @"z:\Downloads\Enscape\Bin64")]
  // [TestCase("GH2",
  //   @"%APPDATA%\McNeel\Rhinoceros\packages\8.0\Grasshopper2\2.0.9040-wip.38379+b5994c78bcff7d30070103a97d764f79221247af",
  //   "C:\\Program Files\\Rhino 9 WIP\\System\\netcore\\RhinoCommon.dll")]
  public Task TestSinglePackage(string name, string path, string rhinoCommonPath) =>
    TestPackage(new DirectoryPackageSource(name, path), "single", rhinoCommonPath);

  #region API break summary

  record ApiBreak(string Package, string Api, string Scope);

  // marker emitted by Pretty.WriteStatus for a failed (broken) instruction
  const string FailMarker = "✗ FAIL ";

  static readonly object s_breaksLock = new object();

  // accumulated breaks per subdir (yak/f4r/single), built up as each package is tested
  static readonly Dictionary<string, List<ApiBreak>> s_breaksBySubdir = new();

  /// <summary>
  /// Parses the compat output for failed instructions that resolve against one of the Rhino
  /// reference assemblies and records them so they can be summarized once all packages are tested.
  /// </summary>
  static void CollectApiBreaks(string subdir, string packageName, string output, ISet<string> rhinoAssemblyNames)
  {
    if (string.IsNullOrEmpty(output))
      return;

    var breaks = new List<ApiBreak>();
    foreach (var line in output.Split('\n'))
    {
      var idx = line.IndexOf(FailMarker, StringComparison.Ordinal);
      if (idx < 0)
        continue;

      var rest = line.Substring(idx + FailMarker.Length).TrimEnd('\r');

      // Pretty.Instruction appends " < <scope>" (the assembly that defines the api)
      var sep = rest.LastIndexOf(" < ", StringComparison.Ordinal);
      if (sep < 0)
        continue;

      var scope = rest.Substring(sep + 3).Trim();
      if (!rhinoAssemblyNames.Contains(scope))
        continue; // only interested in breaks against the Rhino assemblies

      // un-escape the curly braces that the logger doubled up
      var api = rest.Substring(0, sep).Replace("{{", "{").Replace("}}", "}").Trim();
      breaks.Add(new ApiBreak(packageName, api, scope));
    }

    if (breaks.Count == 0)
      return;

    lock (s_breaksLock)
    {
      if (!s_breaksBySubdir.TryGetValue(subdir, out var list))
        s_breaksBySubdir[subdir] = list = new List<ApiBreak>();
      list.AddRange(breaks);
    }
  }

  [OneTimeTearDown]
  public void WriteApiBreakSummary()
  {
    lock (s_breaksLock)
    {
      foreach (var kvp in s_breaksBySubdir)
        WriteApiBreakSummary(kvp.Key, kvp.Value);
    }
  }

  void WriteApiBreakSummary(string subdir, List<ApiBreak> breaks)
  {
    var resultsPath = Path.Combine(ResultsPath, subdir);
    if (!Directory.Exists(resultsPath))
      Directory.CreateDirectory(resultsPath);

    var summaryFile = Path.Combine(resultsPath, "rhino-api-breaks.md");

    var writer = new StringWriter();
    writer.WriteLine($"# Rhino API breaks — {subdir}");
    writer.WriteLine();

    if (breaks.Count == 0)
    {
      writer.WriteLine("No API breaks found against the Rhino assemblies. 🎉");
      File.WriteAllText(summaryFile, writer.ToString());
      return;
    }

    var packages = breaks.Select(b => b.Package).Distinct().OrderBy(p => p).ToList();
    var uniqueApis = breaks.Select(b => (b.Scope, b.Api)).Distinct().Count();

    writer.WriteLine($"**{packages.Count}** package(s) reference **{uniqueApis}** broken API(s).");
    writer.WriteLine();

    // By API: for each broken member, list the packages that use it (these are the breaks
    // most likely to affect lots of plugins, so sort by impact descending)
    writer.WriteLine("## By API");
    writer.WriteLine();
    foreach (var scopeGroup in breaks.GroupBy(b => b.Scope).OrderBy(g => g.Key))
    {
      writer.WriteLine($"### {scopeGroup.Key}");
      writer.WriteLine();
      var apiGroups = scopeGroup
        .GroupBy(b => b.Api)
        .Select(g => new { Api = g.Key, Packages = g.Select(b => b.Package).Distinct().OrderBy(p => p).ToList() })
        .OrderByDescending(g => g.Packages.Count)
        .ThenBy(g => g.Api);

      foreach (var api in apiGroups)
      {
        writer.WriteLine($"- `{api.Api}` — {api.Packages.Count} package(s)");
        foreach (var pkg in api.Packages)
          writer.WriteLine($"  - {pkg}");
      }
      writer.WriteLine();
    }

    // By package: what each package needs that is no longer there
    writer.WriteLine("## By package");
    writer.WriteLine();
    foreach (var pkgGroup in breaks.GroupBy(b => b.Package).OrderBy(g => g.Key))
    {
      var apis = pkgGroup.Select(b => (b.Scope, b.Api)).Distinct().OrderBy(a => a.Scope).ThenBy(a => a.Api).ToList();
      writer.WriteLine($"### {pkgGroup.Key} ({apis.Count} break(s))");
      foreach (var (scope, api) in apis)
        writer.WriteLine($"- `{api}` < {scope}");
      writer.WriteLine();
    }

    File.WriteAllText(summaryFile, writer.ToString());
    Console.WriteLine($"Wrote Rhino API break summary to {summaryFile}");
  }

  #endregion

}

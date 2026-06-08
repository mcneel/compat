namespace CompatTests;

using Compat;
using Microsoft.Win32;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;


public class TestBase
{
  static string? testPath;
  public static string TestPath => testPath ?? (testPath = GetTestPath());

  public static string OSName => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win" : "mac";

  static string? s_rootDir;
  public static string GetRootDir()
  {
    if (s_rootDir == null)
    {
      string? path = null;
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        path = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders", "{374DE290-123F-4565-9164-39C4925E467B}", String.Empty).ToString();

      if (!string.IsNullOrEmpty(path))
      {
        path = Path.Combine(path, "compat-tests");
      }
      else
      {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && dir.Name != "src")
          dir = dir.Parent;
        path = dir?.Parent?.FullName;
      }

      s_rootDir = path ?? Path.Combine(AppContext.BaseDirectory, "..");
    }
    return s_rootDir;
  }

  public static string GetTestPath()
  {
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir != null && dir.Name != "compat")
    {
      dir = dir.Parent;
    }
    if (dir == null)
      throw new InvalidOperationException("test directory not found");

    return Path.Combine(dir.FullName, "test", "integration");
  }

  public static string[] PluginExtensions = new[] { "*.rhp", "*.gha" };
  public static string[] GrasshopperExtensions = new[] { "*.ghcluster", "*.ghuser", "*.gh", "*.ghpy", "*.ghx" };
  public static string[] InstallerExtensions = new[] { "*.exe", "*.msi" };
  public static string[] TemplateExtensions = new[] { "*.3dm" };
  public static string[] AssemblyExtensions = new[] { "*.rhp", "*.gha", "*.dll" };

  public static bool RunningOnWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
  public static bool RunningOnOSX => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
  public static bool RunningInNetCore => Environment.Version.Major >= 5;

  internal static DirectoryInfo? GetRuntimeSpecificFolder(DirectoryInfo root, bool useRootFiles = true, string? pluginFileName = null)
  {
    // root may be a stale path (e.g. an uninstalled package version) - RH-95342.
    // Enumerating a non-existent directory throws DirectoryNotFoundException, so bail out early.
    if (!root.Exists)
      return root;

    // if there are .rhp or .gha's in the top folder, use default behaviour
    if (useRootFiles &&
      (root.GetFiles("*.rhp").Length > 0
      || root.GetFiles("*.gha").Length > 0)
      )
      return root;

    // no plugins to load in this package, scan for framework-specific subfolders if they exist

    if (RunningInNetCore)
    {
      // search from current .NET runtime down to 7 (for future proofing when we support .NET 8, 9, etc.)
      const int minimumNetVersion = 7;
      var currentRuntimeVersion = System.Environment.Version.Major;
      for (int i = currentRuntimeVersion; i >= minimumNetVersion; i--)
      {
        var suffix = RunningOnWindows ? "-windows" :
            RunningOnOSX ? "-macos" :
            null; // -linux

        Version GetOSVersion(string name)
        {
          var idx = name.IndexOf(suffix);
          if (idx < 0)
            return null;

          var versionString = name.Substring(idx + suffix.Length);

          return Version.TryParse(versionString, out var version) ? version : null;
        }

        // search for platform-specific directories first, ordered by os version if specified
        var platformDirs = root
          .GetDirectories($"net{i}.0{suffix}*")
          .OrderByDescending(d => GetOSVersion(d.Name) ?? new Version());

        var currentOSVersion = System.Environment.OSVersion.Version;

        foreach (var platformDir in platformDirs)
        {
          var osVersion = GetOSVersion(platformDir.Name);

          if (pluginFileName != null && !File.Exists(Path.Combine(platformDir.FullName, pluginFileName)))
            continue;

          // no os version, just use it
          if (osVersion == null)
            return platformDir;

          // and ensure the OS version, if specified, is greater or equal to package version
          if (currentOSVersion >= osVersion)
            return platformDir;
        }

        // search for platform-agnostic target
        var targetDir = root.GetDirectories($"net{i}.0")?.FirstOrDefault();
        if (targetDir != null)
        {
          if (pluginFileName != null && !File.Exists(Path.Combine(targetDir.FullName, pluginFileName)))
            continue;
          return targetDir;
        }
      }
    }

    // fall back to the latest net4x folder
    foreach (var net4xdir in root.EnumerateDirectories("net4*").OrderByDescending(r => r.Name))
    {
      var match = Regex.Match(net4xdir.Name, @"net(?<ver>4\d+)");
      if (!match.Success)
        continue;

      if (pluginFileName != null && !File.Exists(Path.Combine(net4xdir.FullName, pluginFileName)))
        continue;

      // just return it.
      return net4xdir;

    }

    // no match, return root folder
    return root;
  }
  public static (int ExitCode, string Output) RunCompatCheck(string pluginPath, string[] referenceAssemblies, bool quiet = false, bool checkAccess = false, bool includeSystemAssemblies = false, bool treatPInvokeAsError = false, bool checkNet10 = false, bool topLevelOnly = true)
  {
    var prevout = Console.Out;
    int exitCode = 0;
    //if (includeSystemAssemblies)
    //{
    //  var corlib = Assembly.Load(new AssemblyName("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")).Location;
    //  var system = Assembly.Load(new AssemblyName("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")).Location;
    //  var systemCore = Assembly.Load(new AssemblyName("System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")).Location;
    //  var systemWinForms = typeof(System.Windows.Forms.Appearance).Assembly.Location;

    //  referenceAssemblies = referenceAssemblies.Concat(new[] { corlib, system, systemCore, systemWinForms }).ToArray();
    //}
    try
    {
      var output = new StringWriter();
      Console.SetOut(output);


      IEnumerable<string> args = Enumerable.Empty<string>();
      if (quiet)
        args = args.Concat(new[] { "-q" });

      if (treatPInvokeAsError)
        args = args.Concat(new[] { "--treat-pinvoke-as-error" });

      if (checkAccess)
        args = args.Concat(new[] { "--check-access" });

      if (checkNet10)
        args = args.Concat(new[] { "--check-net10" });

      if (includeSystemAssemblies)
        args = args.Concat(new[] { "--check-system-assemblies" });

      string pluginDirectory;

      var testCount = 0;

      if (File.Exists(pluginPath))
      {
        pluginDirectory = Path.GetDirectoryName(pluginPath)!;

        // test main plugin assembly
        var asmArgs = args.Concat(new[] { pluginPath }).Concat(referenceAssemblies);
        exitCode = Compat.Program.Main(asmArgs.ToArray());
        testCount++;
      }
      else
        pluginDirectory = pluginPath;


      var folder = GetRuntimeSpecificFolder(new DirectoryInfo(pluginDirectory), useRootFiles: true, pluginFileName: null);
      if (folder == null || !folder.Exists)
      {
        output.WriteLine($"Could not find any assemblies to test in {pluginDirectory}");
        exitCode = -1;
        return (exitCode, output.ToString());
      }

      if (!topLevelOnly)
      {
        // find the first subfolder that contains a plugin assembly and use that as the folder to test
        // lots of f4r packages put their plugin assemblies in a subfolder, so we need to find that folder and test it instead of the root folder
        var subfolderWithPlugin = folder.GetDirectories("*", SearchOption.AllDirectories)
          .FirstOrDefault(d => d.GetFiles("*.rhp", SearchOption.TopDirectoryOnly).Length > 0 || d.GetFiles("*.gha", SearchOption.TopDirectoryOnly).Length > 0);
        if (subfolderWithPlugin != null)
        {
          output.WriteLine($"Found subfolder with plugin assembly: {subfolderWithPlugin.FullName}");
          folder = subfolderWithPlugin;
        }
      }

      // Test all other assemblies in the same path
      output.WriteLine($"Testing assemblies in {folder.FullName}");

      var lastWriteFile = folder.GetFiles("*.*", SearchOption.AllDirectories).Where(r => r.Name != ".DS_Store").OrderBy(f => f.LastWriteTime).LastOrDefault();
      if (lastWriteFile != null)
        output.WriteLine($"Last creation time of any file in folder: {lastWriteFile.LastWriteTime} ({lastWriteFile.Name})");
      output.WriteLine();

      foreach (var ext in AssemblyExtensions)
      {
        var dlls = folder.GetFiles(ext, SearchOption.TopDirectoryOnly);
        foreach (var dllInfo in dlls)
        {
          var dll = dllInfo.FullName;
          if (dll == pluginPath)
            continue;

          var asmArgs = args.Concat(new[] { dll }).Concat(referenceAssemblies);

          var code = Compat.Program.Main(asmArgs.ToArray());
          if (exitCode == 0 || exitCode == Program.ERROR_WARNING || exitCode == Program.ERROR_NOT_DOTNET)
            exitCode = code;

          output.WriteLine($"Exit Code: {code}");

          output.WriteLine();
          testCount++;
        }
      }

      if (testCount == 0)
      {
        output.WriteLine("Could not find any assemblies to test!");
        exitCode = -1;
      }

      var outputString = output.ToString();
      // prevout.WriteLine(outputString);
      return (exitCode, outputString);
    }
    finally
    {
      Console.SetOut(prevout);
    }
  }

  public static string GetTestProject(string assemblyName, string? projectName = null, string? subDir = null)
  {
#if DEBUG
    var config = "Debug";
#else
    var config = "Release";
#endif
    var frameworks = new[] { "net45", "net46", "net461", "net47", "net48" };

    string assemblyProject;
    if (Path.HasExtension(assemblyName))
    {
      assemblyProject = Path.GetFileNameWithoutExtension(assemblyName);
    }
    else
    {
      assemblyProject = assemblyName;
      assemblyName = $"{assemblyName}.dll";
    }

    projectName ??= assemblyProject;

    foreach (var framework in frameworks)
    {
      var path = Path.Combine(TestPath, "projects", projectName, assemblyProject, "bin", config, framework);
      if (subDir != null)
        path = Path.Combine(path, subDir);

      var assemblyFile = Path.Combine(path, assemblyName);
      if (File.Exists(assemblyFile))
        return assemblyFile;
    }

    throw new FileNotFoundException($"Could not find {assemblyName}");
  }

  public static string GetTestFile(string fileName)
  {
    return Path.Combine(TestPath, "files", fileName);
  }

  public static string GetRhinoCommon(string version)
  {
    return Path.Combine(TestPath, "lib", version, "RhinoCommon.dll");
  }

  internal static string GetStatusText(Compat.Program.ResolutionStatus status)
  {
    switch (status)
    {
      case Compat.Program.ResolutionStatus.Success:
        return "PASS";
      case Compat.Program.ResolutionStatus.Failure:
        return "FAIL";
      case Compat.Program.ResolutionStatus.Skipped:
        return "SKIP";
      case Compat.Program.ResolutionStatus.PInvoke:
        return "PINV";
      case Compat.Program.ResolutionStatus.Warning:
        return "WARN";
      default:
        throw new NotSupportedException();
    }
  }
}

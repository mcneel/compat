namespace CompatTests;

using Compat;
using Microsoft.Win32;
using System.Reflection;
using System.Runtime.InteropServices;

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
  public static string[] AssemblyExtensions = new[] { "*.rhp", "*.gha", "*.dll" };

  public static (int ExitCode, string Output) RunCompatCheck(string pluginPath, string[] referenceAssemblies, bool quiet = false, bool checkAccess = false, bool includeSystemAssemblies = false, bool treatPInvokeAsError = false)
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



      // Test all other assemblies in the same path
      if (Directory.Exists(pluginPath))
      {
        foreach (var ext in AssemblyExtensions)
        {
          var dlls = Directory.GetFiles(pluginPath, ext, SearchOption.TopDirectoryOnly);
          foreach (var dll in dlls)
          {
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
      default:
        throw new NotSupportedException();
    }
  }
}

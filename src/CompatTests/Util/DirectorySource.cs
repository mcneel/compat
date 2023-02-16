using System.Collections;

namespace CompatTests.Util
{
  public class DirectoryPackageSource : IPackageSource
  {
    public DirectoryPackageSource(string name, string path)
    {
      Name = name;
      Path = path;
    }

    public string Name { get; }
    public string Path { get; }
    public Task<string> Download() => Task.FromResult(Path);

    public override string ToString() => Name;
  }

  public static class DirectorySource
  {
    public static IEnumerable<IPackageSource> Get(string path, bool addOSName = true)
    {
      if (!Path.IsPathRooted(path))
      {
        path = Path.Combine(AppContext.BaseDirectory, path);
      }

      if (addOSName)
      {
        path = Path.Combine(path, TestBase.OSName);
      }

      if (!Directory.Exists(path))
        yield break;

      foreach (var dir in Directory.GetDirectories(path).OrderBy(r => Path.GetFileName(r)))
      {
        var name = Path.GetFileName(dir);

        // get the first path with assemblies and skip any subdirectories
        var dllPath = TestBase.AssemblyExtensions
          .SelectMany(ext => Directory.GetFiles(dir, ext, SearchOption.AllDirectories))
          .GroupBy(r => Path.GetDirectoryName(r))
          .Select(r => r.Key)
          .OrderBy(r => r?.Length ?? int.MaxValue)
          .FirstOrDefault();

        if (dllPath != null)
          yield return new DirectoryPackageSource(name, dllPath);
      }
    }
  }
}

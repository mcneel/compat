using System.Collections;

namespace CompatTests.Util
{
  public abstract class BaseSource : IEnumerable
  {
    public bool UseRemoteSource { get; }

    public string Id { get; }
    public BaseSource(string id, bool useRemoteSource = false)
    {
      Id = id;
      OutputPath = Path.Combine(TestBase.GetRootDir(), "packages", Id, TestBase.OSName);
      UseRemoteSource = useRemoteSource;
    }


    public string OutputPath { get; set; }
    public virtual async Task DownloadAll()
    {
      await foreach (var source in GetPackages())
      {
        try
        {
          await source.Download();
        }
        catch (Exception ex)
        {
          Console.WriteLine($"Warn package '{source.Name}': {ex}");
        }
      }
    }

    public IEnumerator GetEnumerator() => Get().GetEnumerator();

    IEnumerable<IPackageSource> Get()
    {
      // already downloaded packages? cool, let's not do it again.
      if (Directory.Exists(OutputPath) && Directory.EnumerateFiles(OutputPath, "*.*", SearchOption.AllDirectories).Any())
      {
        List<IPackageSource> dirs = new List<IPackageSource>();
        foreach (var dir in Directory.EnumerateDirectories(OutputPath).OrderBy(r => Path.GetFileName(r)))
        {
          var name = Path.GetFileName(dir);
          foreach (var subdir in Directory.EnumerateDirectories(dir).OrderBy(r => Path.GetFileName(r)))
          {
            var subname = Path.GetFileName(subdir);
            dirs.Add(new DirectoryPackageSource(name, subdir));
          }
        }
        return dirs;
      }
        

      if (UseRemoteSource)
      {
        return Task.Run(async () =>
        {
          var list = new List<IPackageSource>();
          await foreach (var package in GetPackages())
          {
            list.Add(package);
          }
          return list;
        }).GetAwaiter().GetResult();
      }
      return Enumerable.Empty<IPackageSource>();
    }

    public abstract IAsyncEnumerable<IPackageSource> GetPackages();

  }
}

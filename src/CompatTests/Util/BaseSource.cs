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
      OutputPath = Path.Combine(AppContext.BaseDirectory, "..", Id, TestBase.OSName);
      UseRemoteSource = useRemoteSource;
    }

    public string OutputPath { get; set; }
    public virtual async Task DownloadAll()
    {
      await foreach (var source in GetPackages())
      {
        await source.Download();
      }
    }

    public IEnumerator GetEnumerator() => Get().GetEnumerator();

    IEnumerable<IPackageSource> Get()
    {
      // already downloaded packages? cool, let's not do it again.
      if (Directory.Exists(OutputPath) && Directory.EnumerateFiles(OutputPath, "*.*", SearchOption.AllDirectories).Any())
        return DirectorySource.Get(OutputPath, false);

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

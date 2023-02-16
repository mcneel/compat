using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Yak;

namespace CompatTests.Util
{
  public interface IPackageSource
  {
    string Name { get; }
    Task<string> Download();
  }

  public class YakPackageSource : IPackageSource
  {
    Package _package;
    YakClient _yak;
    public YakPackageSource(YakClient yak, Package package)
    {
      _yak = yak;
      _package = package;
    }

    public string Name => _package.Name;

    public async Task<string> Download()
    {
      if (!Directory.Exists(_yak.PackageFolder))
        Directory.CreateDirectory(_yak.PackageFolder);

      var outputPath = Path.Combine(_yak.PackageFolder, _package.Name, _package.Version);

      // only download/install if it doesn't already exist
      if (!Directory.Exists(outputPath))
      {
        var tempPath = await _yak.Version.Download(_package.Name, _package.Version);
        _yak.Install(tempPath, out var manifest);
      }

      return outputPath;
    }

    public override string ToString() => _package.Name;
  }

  public class YakSource : IEnumerable
  {
    static string YakUrl = "https://yak.rhino3d.com/";

    internal static YakClient CreateYakClient(string name = "compat_tests", bool strict = false)
    {
      var pinfo = new ProductHeaderValue(name);
      var source_strs = YakUrl.Split(';');
      var sources = new List<IPackageRepository>();
      foreach (var s in source_strs)
      {
        // TODO: pass strict somehow.
        sources.Add(PackageRepositoryFactory.Create(s, new HttpClient(), pinfo));
      }
      var yak = new YakClient(sources.ToArray());
      return yak;
    }


    public IEnumerator GetEnumerator() => Get().GetEnumerator();

    public static IEnumerable<IPackageSource> Get()
    {
      var packageFolder = Path.Combine(AppContext.BaseDirectory, "yak", TestBase.OSName);

      // already downloaded packages? cool, let's not do it again.
      if (Directory.Exists(packageFolder) && Directory.EnumerateFiles(packageFolder, "*.*", SearchOption.AllDirectories).Any())
        return DirectorySource.Get(packageFolder, false);

      var list = new List<IPackageSource>();
      Task.Run(async () =>
      {
        var strict = false;
        var yak = CreateYakClient(strict: strict);
        yak.PackageFolder = packageFolder;

        foreach (var package in await yak.Package.GetAll())
        {
          var ver = await yak.Version.Get(package.Name, package.Version);

          var dist = ver.Distributions.FirstOrDefault(d => d.IsCompatible(strict));

          // no compatible distribution, go to next.
          if (dist == null)
            continue;

          list.Add(new YakPackageSource(yak, package));
        }
      }).GetAwaiter().GetResult();
      return list;
    }
  }
}

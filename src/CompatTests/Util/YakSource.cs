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

  public class YakSource : BaseSource
  {
    static string YakUrl = "https://yak.rhino3d.com/";
    public YakSource() : base("yak")
    {
    }

    internal static YakClient CreateYakClient(string name = "compat_tests", bool strict = false)
    {
      var pinfo = new ProductHeaderValue(name);
      var source_strs = YakUrl.Split(';');
      var sources = new List<IPackageRepository>();
      foreach (var s in source_strs)
      {
        // TODO: pass strict somehow.
        var source = PackageRepositoryFactory.Create(s, new HttpClient(), pinfo);
        
        // allow downloading v7 packages
        if (source is ApiPackageRepository apiSource)
          apiSource.Strict = strict;
        
        sources.Add(source);
      }
      var yak = new YakClient(sources.ToArray());
      return yak;
    }

    public override async IAsyncEnumerable<IPackageSource> GetPackages()
    {
      var strict = false;
      var yak = CreateYakClient(strict: strict);
      yak.PackageFolder = OutputPath;

      foreach (var package in await yak.Package.GetAll())
      {
        var ver = await yak.Version.Get(package.Name, package.Version);

        var dist = ver.Distributions.FirstOrDefault(d => d.IsCompatible(strict));

        // no compatible distribution, go to next.
        if (dist == null)
          continue;

        yield return new YakPackageSource(yak, package);
      }
    }
  }
}

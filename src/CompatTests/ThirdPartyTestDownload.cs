using CompatTests.Util;

namespace CompatTests;

[TestFixture]
public class ThirdPartyTestDownload : TestBase
{
  [Test]
  [TestCase(typeof(YakSource))]
  [TestCase(typeof(Food4RhinoSource))]
  public async Task DownloadPackages(Type type)
  {
    return;
    if (Activator.CreateInstance(type) is BaseSource source)
    {
      await source.DownloadAll();
    }
  }

}

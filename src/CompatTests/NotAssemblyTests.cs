namespace CompatTests;

public class NotAssemblyTests : TestBase
{
  [Test]
  public void ZipArchiveShouldFailWithNotDotNet()
  {
    var exitCode = Compat.Program.Main(new [] { GetTestFile("test.zip"), GetRhinoCommon("rhino_en-us_6.0.16176.11441") });

    Assert.That(exitCode, Is.EqualTo(Compat.Program.ERROR_NOT_DOTNET), "should not work with zip archives");
  }
  
  [Test]
  public void DirectoryShouldFailWithNotDotNet()
  {
    var exitCode = Compat.Program.Main(new [] { TestPath, GetRhinoCommon("rhino_en-us_6.0.16176.11441") });

    Assert.That(exitCode, Is.EqualTo(Compat.Program.ERROR_NOT_DOTNET), "should not work with directories");
  }
}

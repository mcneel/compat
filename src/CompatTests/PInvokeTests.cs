namespace CompatTests;

public class PInvokeTests : TestBase
{
  [Test]
  public void PInvokesShouldBeDetected()
  {
    var result = RunCompatCheck(GetTestProject("PInvokeExample.exe"), new [] { GetRhinoCommon("rhino_en-us_6.0.16176.11441") });

    Assert.That(result.ExitCode, Is.EqualTo(0), "pinvokes should not fail");

    Assert.That(result.Output, Contains.Substring("PINV System.Int32 PlatformInvokeTest::puts(System.String) < msvcrt.dll"), "should detect pinvokes");
    Assert.That(result.Output, Contains.Substring("PINV System.Int32 PlatformInvokeTest::_flushall() < msvcrt.dll"), "should detect pinvokes");
  }

  [Test]
  public void PInvokesShouldCauseErrorWhenSwitchEnabled()
  {
    var result = RunCompatCheck(GetTestProject("PInvokeExample.exe"), new [] { GetRhinoCommon("rhino_en-us_6.0.16176.11441") }, treatPInvokeAsError: true);

    Assert.That(result.ExitCode, Is.EqualTo(Compat.Program.ERROR_PINVOKE), "pinvokes should fail when option selected");
  }
}

namespace CompatTests;

public class RegressionTests : TestBase
{
  [Test]
  public void ShouldResolveMultidimensionalArrays()
  {
    var result = RunCompatCheck(GetTestProject("2darray_test", "rdktest"), new [] { GetRhinoCommon("rhino_en-us_6.0.16176.11441") });

    Assert.That(result.ExitCode, Is.EqualTo(0), "should successfully resolve multidimensional arrays");
  }
  
  [Test]
  public void ShouldResolveMultidimensionalArraysWithSystemAssemblies()
  {
    var result = RunCompatCheck(GetTestProject("2darray_test", "rdktest"), new [] { GetRhinoCommon("rhino_en-us_6.0.16176.11441") }, includeSystemAssemblies: true);
    
    if (result.ExitCode != 0)
      Console.WriteLine(result.Output);

    Assert.That(result.ExitCode, Is.EqualTo(0), "should successfully resolve multidimensional arrays");
  }
}

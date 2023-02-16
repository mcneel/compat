namespace CompatTests;

public class OverrideTests : TestBase
{
  [Test]
  public void PassWhenAbstractMethodsOfBaseClassImplemented()
  {
    var result = RunCompatCheck(GetTestProject("rdktest"), new [] { GetRhinoCommon("rhino_en-us_6.0.16176.11441") });

    Assert.That(result.ExitCode, Is.EqualTo(0), "should exit with 0 if all abstract methods of the base class are implemented in the derived class");
    
    Assert.IsTrue(result.Output.Contains("Overrides (Rhino.PlugIns.RenderPlugIn)"));
    Assert.IsTrue(result.Output.Contains("✓ PASS Rhino.Commands.Result Rhino.PlugIns.RenderPlugIn::Render(Rhino.RhinoDoc,Rhino.Commands.RunMode,Rhino.PlugIns.RenderPlugIn/RenderOptions) < RhinoCommon"));
  }

  [Test]
  public void FailWhenOverriddenMethodsHaveChanged()
  {
    var result = RunCompatCheck(GetTestProject("rdktest"), new [] { GetRhinoCommon("rhino_en-us_6.0.16231.01091") });

    Assert.That(result.ExitCode, Is.EqualTo(112), "should fail if the signatures of overridden abstract methods have changed");
    Assert.IsTrue(result.Output.Contains("✗ FAIL Rhino.Commands.Result Rhino.PlugIns.RenderPlugIn::Render(Rhino.RhinoDoc,Rhino.Commands.RunMode,System.Boolean) < RhinoCommon"));
  }
  

  [Test]
  public void ShouldPassWhenOverridesClassIsAbstract()
  {
    var result = RunCompatCheck(GetTestProject("rdktest_abstract", "rdktest"), new [] { GetRhinoCommon("rhino_en-us_6.0.16231.01091") });

    Assert.That(result.ExitCode, Is.EqualTo(0), "should not fail during overrides if class is itself abstract");
  }
  

  [Test]
  public void ShouldPassWithFsharpCompilerService()
  {
    var result = RunCompatCheck(GetTestFile("FSharp.Compiler.Service.dll"), new [] { GetRhinoCommon("rhino_en-us_6.0.16231.01091") });

    Assert.That(result.ExitCode, Is.EqualTo(0), "should not fail during overrides if class is itself abstract");
  }

//  RH-41841
//  TODO: create mock assembly
//  @test "should not fail during overrides if base class is generic" {
  //  run dotnet ../../bin/Release/net7.0/Compat.dll --quiet files/FSharp.Compiler.Service.dll
  //  echo "$output"
  //  [ "$status" -eq 0 ]
//  }
  
}

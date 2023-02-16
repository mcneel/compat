namespace CompatTests;

public class AccessTests : TestBase
{
  [Test]
  public void PassWhenReferencingAccessibleApis()
  {
    var result = RunCompatCheck(GetTestProject("AccessTest"), new [] { GetTestProject("AccessTestLib", "AccessTest") }, checkAccess: true);

    Assert.That(result.ExitCode, Is.EqualTo(0), "should exit with 0 when referencing accessible apis");
  }

  [Test]
  public void ShouldPassWhenCheckAccessIsDisabled()
  {
    var result = RunCompatCheck(GetTestProject("AccessTest"), new[] { GetTestProject("AccessTestLib", "AccessTest", "noaccess") }, checkAccess: false);

    Assert.That(result.ExitCode, Is.EqualTo(0), "should exit with 0 when referencing inaccessible apis when check access is disabled");
  }

  [Test]
  public void ShouldFailWhenReferencingInaccessibleApis()
  {
    var result = RunCompatCheck(GetTestProject("AccessTest"), new [] { GetTestProject("AccessTestLib", "AccessTest", "noaccess") }, checkAccess: true);

    Assert.That(result.ExitCode, Is.EqualTo(Compat.Program.ERROR_COMPAT), "should catch all cases of members failing to resolve due to their accessibility");

    Assert.That(result.Output, Contains.Substring("✗ FAIL System.Void AccessTestLib.MyClass::PublicMethodBecomesPrivate() < AccessTestLib"), "should fail if public method becomes private");
    
    Assert.That(result.Output, Contains.Substring("✗ FAIL System.Void AccessTestLib.MyClass::PublicMethodBecomesProtected() < AccessTestLib"), "should fail if public method becomes protected");

    Assert.That(result.Output, Contains.Substring("✗ FAIL System.Void AccessTestLib.MyClass::PublicMethodBecomesInternal() < AccessTestLib"), "should fail if public method becomes internal");

    Assert.That(result.Output, Contains.Substring("✗ FAIL System.Void AccessTestLib.MyClass::PublicMethodBecomesProtectedInternal() < AccessTestLib"), "should fail if public method becomes protected internal");

    Assert.That(result.Output, Contains.Substring("✓ PASS System.Boolean AccessTestLib.MyClass::PublicFieldBecomesProtected < AccessTestLib"), "should NOT fail if public field becomes protected");

    Assert.That(result.Output, Contains.Substring("✗ FAIL System.Boolean AccessTestLib.MyClass::PublicFieldBecomesInternal < AccessTestLib"), "should fail if public field becomes internal");

    Assert.That(result.Output, Contains.Substring("✗ FAIL System.Void AccessTestLib.MyClass::set_PublicPropertyBecomesPrivate(System.Boolean) < AccessTestLib"), "should fail if public property becomes private");

    Assert.That(result.Output, Contains.Substring("✗ FAIL System.Void AccessTestLib.MyClass::set_PublicPropertyBecomesProtected(System.Boolean) < AccessTestLib"), "should fail if public property becomes protected");

    Assert.That(result.Output, Contains.Substring("✗ FAIL System.Void AccessTestLib.MyClass::set_PublicPropertyBecomesInternal(System.Boolean) < AccessTestLib"), "should fail if public property becomes internal");
    
    Assert.That(result.Output, Contains.Substring("✗ FAIL AccessTestLib.PublicClassBecomesPrivate < AccessTestLib"), "should fail if public class becomes private");
    Assert.That(result.Output, Contains.Substring("✗ FAIL System.Void AccessTestLib.PublicClassBecomesPrivate::.ctor() < AccessTestLib"), "should fail if public class becomes private");
    
    Assert.That(result.Output, Contains.Substring("✗ FAIL AccessTestLib.PublicClassBecomesInternal < AccessTestLib"), "should fail if public class becomes internal");
    Assert.That(result.Output, Contains.Substring("✗ FAIL System.Void AccessTestLib.PublicClassBecomesInternal::.ctor() < AccessTestLib"), "should fail if public class becomes internal");
    
    Assert.That(result.Output, Contains.Substring("✗ FAIL AccessTestLib.MyClass/NestedClassBecomesPrivate < AccessTestLib"), "should fail if public nested class becomes private");
    Assert.That(result.Output, Contains.Substring("✗ FAIL System.Void AccessTestLib.MyClass/NestedClassBecomesPrivate::.ctor() < AccessTestLib"), "should fail if public nested class becomes private");

    Assert.That(result.Output, Contains.Substring("✗ FAIL AccessTestLib.MyClass/NestedClassBecomesProtected < AccessTestLib"), "should fail if public nested class becomes protected");
    Assert.That(result.Output, Contains.Substring("✗ FAIL System.Void AccessTestLib.MyClass/NestedClassBecomesProtected::.ctor() < AccessTestLib"), "should fail if public nested class becomes protected");

    Assert.That(result.Output, Contains.Substring("✗ FAIL AccessTestLib.MyClass/NestedClassBecomesInternal < AccessTestLib"), "should fail if public nested class becomes internal");
    Assert.That(result.Output, Contains.Substring("✗ FAIL System.Void AccessTestLib.MyClass/NestedClassBecomesInternal::.ctor() < AccessTestLib"), "should fail if public nested class becomes internal");

    Assert.That(result.Output, Contains.Substring("✗ FAIL System.Void AccessTestLib.OuterClassBecomesPrivate/NestedClass::.ctor() < AccessTestLib"), "should fail if outer class of public nested class becomes private");
    Assert.That(result.Output, Contains.Substring("✗ FAIL AccessTestLib.OuterClassBecomesPrivate/NestedClass < AccessTestLib"), "should fail if outer class of public nested class becomes private");
    
    Assert.That(result.Output, Contains.Substring("✗ FAIL AccessTestLib.PublicStructBecomesPrivate < AccessTestLib"), "should fail if public struct becomes private");
    
    Assert.That(result.Output, Contains.Substring("✗ FAIL AccessTestLib.PublicStructBecomesInternal < AccessTestLib"), "should fail if public struct becomes internal");

    Assert.That(result.Output, Contains.Substring("✓ PASS System.Void AccessTestLib.MyClass::PublicMethodBecomesProtected() < AccessTestLib"), "should pass if public method becomes protected when called from derived class");

    Assert.That(result.Output, Contains.Substring("✓ PASS System.Boolean AccessTestLib.MyClass::PublicFieldBecomesProtected < AccessTestLib"), "should pass if public field becomes protected when called from derived class");

    Assert.That(result.Output, Contains.Substring("✓ PASS System.Void AccessTestLib.MyClass::set_PublicPropertyBecomesProtected(System.Boolean) < AccessTestLib"), "should pass if public property becomes protected when called from derived class");
    
    Assert.That(result.Output, Contains.Substring("✓ PASS System.Void AccessTestLib.MyClass/NestedClassBecomesProtected::.ctor() < AccessTestLib"), "should pass if public nested class becomes protected when called from derived class");
    Assert.That(result.Output, Contains.Substring("✓ PASS AccessTestLib.MyClass/NestedClassBecomesProtected < AccessTestLib"), "should pass if public nested class becomes protected when called from derived class");

    Assert.That(result.Output, Contains.Substring("✓ PASS System.Void AccessTestLib.MyClass::PublicMethodBecomesProtectedInternal() < AccessTestLib"), "should pass if public nested class becomes protected internal when called from derived class");

    Assert.That(result.Output, Contains.Substring("✓ PASS System.Void AccessTestLib.BaseClassWithProtectedVirtualMethod::ProtectedVirtualMethod() < AccessTestLib"), "should pass if protected method called from double derived class");
  }

}

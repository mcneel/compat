#!/usr/bin/env bats

function setup {
  cd ${COMPAT_ROOT}/test/integration
  xbuild projects/AccessTest/AccessTest.sln /t:Rebuild
  xbuild projects/AccessTest/AccessTestLib/AccessTestLib.csproj /t:Rebuild /p:DefineConstants=ACCESS
  /usr/local/bin/mono ../../bin/Release/Compat.exe projects/AccessTest/AccessTest/bin/Debug/AccessTest.dll projects/AccessTest/AccessTestLib/bin/Debug/AccessTestLib.dll > test_output || status=$?
  cat test_output
}

function teardown {
  rm test_output
}

# @test "should exit with 0 if all abstract methods of the base class are implemented in the derived class" {
#   run mono ../../bin/Release/Compat.exe projects/rdktest/rdktest/bin/Debug/rdktest.dll lib/rhino_en-us_6.0.16176.11441/RhinoCommon.dll
#   echo "$output"
#   [ "$status" -eq 0 ]
#   echo "$output" | grep "Overrides (Rhino.PlugIns.RenderPlugIn)"
#   echo "$output" | grep "✓ Rhino.Commands.Result Rhino.PlugIns.RenderPlugIn::Render(Rhino.RhinoDoc,Rhino.Commands.RunMode,Rhino.PlugIns.RenderPlugIn/RenderOptions) < RhinoCommon.dll"
# }
#
# @test "should fail if the signatures of overridden abstract methods have changed" {
#   run mono ../../bin/Release/Compat.exe projects/rdktest/rdktest/bin/Debug/rdktest.dll lib/rhino_en-us_6.0.16231.01091/RhinoCommon.dll
#   echo "$output"
#   [ "$status" -eq 112 ]
# }
#
# @test "should fail if members fail to resolve due to their accessibility" {
#   # /usr/local/bin/mono ../../bin/Release/Compat.exe projects/AccessTest/AccessTest/bin/Debug/AccessTest.dll projects/AccessTest/AccessTestLib/bin/Debug/AccessTestLib.dll > test_output || status=$?
#   # cat test_output
#   echo $hey
#   # echo "$output"
#   echo $status
#   [ "$status" -eq 112 ]
#   # echo "$output" | grep "✗ System.Void AccessTestLib.MyClass::PublicMethodBecomesPrivate() < AccessTestLib"
#   # echo "$output" | grep "✗ System.Void AccessTestLib.MyClass::PublicMethodBecomesProtected() < AccessTestLib"
#   # echo "$output" | grep "✗ System.Void AccessTestLib.MyClass::PublicMethodBecomesInternal() < AccessTestLib"
#   # echo "$output" | grep "✗ System.Boolean AccessTestLib.MyClass::PublicFieldBecomesPrivate < AccessTestLib"
#   # echo "$output" | grep "✗ System.Boolean AccessTestLib.MyClass::PublicFieldBecomesProtected < AccessTestLib"
#   # echo "$output" | grep "✗ System.Boolean AccessTestLib.MyClass::PublicFieldBecomesInternal < AccessTestLib"
#   # echo "$output" | grep "✗ System.Void AccessTestLib.MyClass::set_PublicPropertyBecomesPrivate(System.Boolean) < AccessTestLib"
#   # echo "$output" | grep "✗ System.Void AccessTestLib.MyClass::set_PublicPropertyBecomesProtected(System.Boolean) < AccessTestLib"
#   # echo "$output" | grep "✗ System.Void AccessTestLib.MyClass::set_PublicPropertyBecomesInternal(System.Boolean) < AccessTestLib"
#   #
#   # echo "$output" | grep "✓ System.Void AccessTestLib.MyClass::PublicMethodBecomesProtected() < AccessTestLib"
#   # echo "$output" | grep "✓ System.Boolean AccessTestLib.MyClass::PublicFieldBecomesProtected < AccessTestLib"
#   # echo "$output" | grep "✓ System.Void AccessTestLib.MyClass::set_PublicPropertyBecomesProtected(System.Boolean) < AccessTestLib"
#
#   cat test_output | grep "✗ System.Void AccessTestLib.MyClass::PublicMethodBecomesPrivate() < AccessTestLib"
#   cat test_output | grep "✗ System.Void AccessTestLib.MyClass::PublicMethodBecomesProtected() < AccessTestLib"
#   cat test_output | grep "✗ System.Void AccessTestLib.MyClass::PublicMethodBecomesInternal() < AccessTestLib"
#   cat test_output | grep "✗ System.Boolean AccessTestLib.MyClass::PublicFieldBecomesPrivate < AccessTestLib"
#   cat test_output | grep "✗ System.Boolean AccessTestLib.MyClass::PublicFieldBecomesProtected < AccessTestLib"
#   cat test_output | grep "✗ System.Boolean AccessTestLib.MyClass::PublicFieldBecomesInternal < AccessTestLib"
#   cat test_output | grep "✗ System.Void AccessTestLib.MyClass::set_PublicPropertyBecomesPrivate(System.Boolean) < AccessTestLib"
#   cat test_output | grep "✗ System.Void AccessTestLib.MyClass::set_PublicPropertyBecomesProtected(System.Boolean) < AccessTestLib"
#   cat test_output | grep "✗ System.Void AccessTestLib.MyClass::set_PublicPropertyBecomesInternal(System.Boolean) < AccessTestLib"
#
#   cat test_output | grep "✓ System.Void AccessTestLib.MyClass::PublicMethodBecomesProtected() < AccessTestLib"
#   cat test_output | grep "✓ System.Boolean AccessTestLib.MyClass::PublicFieldBecomesProtected < AccessTestLib"
#   cat test_output | grep "✓ System.Void AccessTestLib.MyClass::set_PublicPropertyBecomesProtected(System.Boolean) < AccessTestLib"
# }
@test "should fail if public method becomes private" {
  cat test_output | grep "✗ System.Void AccessTestLib.MyClass::PublicMethodBecomesPrivate() < AccessTestLib"
}

@test "should fail if public method becomes protected" {
  cat test_output | grep "✗ System.Void AccessTestLib.MyClass::PublicMethodBecomesProtected() < AccessTestLib"
}

@test "should fail if public method becomes internal" {
  cat test_output | grep "✗ System.Void AccessTestLib.MyClass::PublicMethodBecomesInternal() < AccessTestLib"
}

@test "should fail if public field becomes private" {
  cat test_output | grep "✗ System.Boolean AccessTestLib.MyClass::PublicFieldBecomesPrivate < AccessTestLib"
}

@test "should fail if public field becomes protected" {
  cat test_output | grep "✗ System.Boolean AccessTestLib.MyClass::PublicFieldBecomesProtected < AccessTestLib"
}

@test "should fail if public field becomes internal" {
  cat test_output | grep "✗ System.Boolean AccessTestLib.MyClass::PublicFieldBecomesInternal < AccessTestLib"
}

@test "should fail if public property becomes private" {
  cat test_output | grep "✗ System.Void AccessTestLib.MyClass::set_PublicPropertyBecomesPrivate(System.Boolean) < AccessTestLib"
}

@test "should fail if public property becomes protected" {
  cat test_output | grep "✗ System.Void AccessTestLib.MyClass::set_PublicPropertyBecomesProtected(System.Boolean) < AccessTestLib"
}

@test "should fail if public property becomes internal" {
  cat test_output | grep "✗ System.Void AccessTestLib.MyClass::set_PublicPropertyBecomesInternal(System.Boolean) < AccessTestLib"
}

@test "should fail if public class becomes private" {
  skip "Not yet implemented"
  cat test_output | grep "✓ System.Void AccessTestLib.PublicClassBecomesPrivate::.ctor() < AccessTestLib"
}
@test "should fail if public class becomes internal" {
  skip "Not yet implemented"
  cat test_output | grep "✓ System.Void AccessTestLib.PublicClassBecomesInternal::.ctor() < AccessTestLib"
}

@test "should pass if public method becomes protected when called from derived class" {
  cat test_output | grep "✓ System.Void AccessTestLib.MyClass::PublicMethodBecomesProtected() < AccessTestLib"
}

@test "should pass if public field becomes internal when called from derived class" {
  cat test_output | grep "✓ System.Boolean AccessTestLib.MyClass::PublicFieldBecomesProtected < AccessTestLib"
}

@test "should pass if public property becomes internal when called from derived class" {
  cat test_output | grep "✓ System.Void AccessTestLib.MyClass::set_PublicPropertyBecomesProtected(System.Boolean) < AccessTestLib"
}

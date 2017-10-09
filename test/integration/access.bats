#!/usr/bin/env bats

function setup {
  cd ${COMPAT_ROOT}/test/integration
  msbuild projects/AccessTest/AccessTest.sln /t:Rebuild > test_output || cat test_output
  msbuild projects/AccessTest/AccessTestLib/AccessTestLib.csproj /t:Rebuild /p:DefineConstants=ACCESS > test_output || cat test_output
  mono ../../bin/Release/Compat.exe projects/AccessTest/AccessTest/bin/Debug/AccessTest.dll projects/AccessTest/AccessTestLib/bin/Debug/AccessTestLib.dll > test_output || status=$?
  cat test_output
}

function teardown {
  rm test_output
}

@test "should catch all cases of members failing to resolve due to their accessibility" {
  [ "$status" -eq 112 ]
# }

# @test "should fail if public method becomes private" {
  cat test_output | grep "✗ System.Void AccessTestLib.MyClass::PublicMethodBecomesPrivate() < AccessTestLib"
# }

# @test "should fail if public method becomes protected" {
  cat test_output | grep "✗ System.Void AccessTestLib.MyClass::PublicMethodBecomesProtected() < AccessTestLib"
# }

# @test "should fail if public method becomes internal" {
  cat test_output | grep "✗ System.Void AccessTestLib.MyClass::PublicMethodBecomesInternal() < AccessTestLib"
# }

# @test "should fail if public method becomes protected internal" {
  cat test_output | grep "✗ System.Void AccessTestLib.MyClass::PublicMethodBecomesProtectedInternal() < AccessTestLib"
# }

# @test "should fail if public field becomes private" {
  cat test_output | grep "✗ System.Boolean AccessTestLib.MyClass::PublicFieldBecomesPrivate < AccessTestLib"
# }

# @test "should fail if public field becomes protected" {
  cat test_output | grep "✗ System.Boolean AccessTestLib.MyClass::PublicFieldBecomesProtected < AccessTestLib"
# }

# @test "should fail if public field becomes internal" {
  cat test_output | grep "✗ System.Boolean AccessTestLib.MyClass::PublicFieldBecomesInternal < AccessTestLib"
# }

# @test "should fail if public property becomes private" {
  cat test_output | grep "✗ System.Void AccessTestLib.MyClass::set_PublicPropertyBecomesPrivate(System.Boolean) < AccessTestLib"
# }

# @test "should fail if public property becomes protected" {
  cat test_output | grep "✗ System.Void AccessTestLib.MyClass::set_PublicPropertyBecomesProtected(System.Boolean) < AccessTestLib"
# }

# @test "should fail if public property becomes internal" {
  cat test_output | grep "✗ System.Void AccessTestLib.MyClass::set_PublicPropertyBecomesInternal(System.Boolean) < AccessTestLib"
# }

# @test "should fail if public class becomes private" {
  cat test_output | grep "✗ AccessTestLib.PublicClassBecomesPrivate < AccessTestLib"
  # skip "Not yet implemented"
  cat test_output | grep "✗ System.Void AccessTestLib.PublicClassBecomesPrivate::.ctor() < AccessTestLib"
# }

# @test "should fail if public class becomes internal" {
  cat test_output | grep "✗ AccessTestLib.PublicClassBecomesInternal < AccessTestLib"
  # skip "Not yet implemented"
  cat test_output | grep "✗ System.Void AccessTestLib.PublicClassBecomesInternal::.ctor() < AccessTestLib"
# }

# @test "should fail if public nested class becomes private" {
  cat test_output | grep "✗ AccessTestLib.MyClass/NestedClassBecomesPrivate < AccessTestLib"
  # skip "Not yet implemented"
  cat test_output | grep "✗ System.Void AccessTestLib.MyClass/NestedClassBecomesPrivate::.ctor() < AccessTestLib"
# }

# @test "should fail if public nested class becomes protected" {
  cat test_output | grep "✗ AccessTestLib.MyClass/NestedClassBecomesProtected < AccessTestLib"
  # skip "Not yet implemented"
  cat test_output | grep "✗ System.Void AccessTestLib.MyClass/NestedClassBecomesProtected::.ctor() < AccessTestLib"
# }

# @test "should fail if public nested class becomes internal" {
  cat test_output | grep "✗ AccessTestLib.MyClass/NestedClassBecomesInternal < AccessTestLib"
  # skip "Not yet implemented"
  cat test_output | grep "✗ System.Void AccessTestLib.MyClass/NestedClassBecomesInternal::.ctor() < AccessTestLib"
# }

# @test "should fail if outer class of public nested class becomes private" {
  cat test_output | grep "✗ System.Void AccessTestLib.OuterClassBecomesPrivate/NestedClass::.ctor() < AccessTestLib"
  cat test_output | grep "✗ AccessTestLib.OuterClassBecomesPrivate/NestedClass < AccessTestLib"
# }

# @test "should fail if public struct becomes private" {
  cat test_output | grep "✗ AccessTestLib.PublicStructBecomesPrivate < AccessTestLib"
# }

# @test "should fail if public struct becomes internal" {
  cat test_output | grep "✗ AccessTestLib.PublicStructBecomesInternal < AccessTestLib"
# }

# @test "should pass if public method becomes protected when called from derived class" {
  cat test_output | grep "✓ System.Void AccessTestLib.MyClass::PublicMethodBecomesProtected() < AccessTestLib"
# }

# @test "should pass if public field becomes protected when called from derived class" {
  cat test_output | grep "✓ System.Boolean AccessTestLib.MyClass::PublicFieldBecomesProtected < AccessTestLib"
# }

# @test "should pass if public property becomes protected when called from derived class" {
  cat test_output | grep "✓ System.Void AccessTestLib.MyClass::set_PublicPropertyBecomesProtected(System.Boolean) < AccessTestLib"
# }

# @test "should pass if public nested class becomes protected when called from derived class" {
  cat test_output | grep "✓ System.Void AccessTestLib.MyClass/NestedClassBecomesProtected::.ctor() < AccessTestLib"
  cat test_output | grep "✓ AccessTestLib.MyClass/NestedClassBecomesProtected < AccessTestLib"
# }

# @test "should pass if public nested class becomes protected internal when called from derived class" {
  cat test_output | grep "✓ System.Void AccessTestLib.MyClass::PublicMethodBecomesProtectedInternal() < AccessTestLib"
# }

# @test "should pass if protected method called from double derived class" {
  cat test_output | grep "✓ System.Void AccessTestLib.BaseClassWithProtectedVirtualMethod::ProtectedVirtualMethod() < AccessTestLib"
}

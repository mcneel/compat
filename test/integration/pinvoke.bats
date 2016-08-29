#!/usr/bin/env bats

function setup {
  cd ${COMPAT_ROOT}/test/integration
  xbuild projects/PInvokeExample/PInvokeExample.sln
}

@test "should detect pinvokes" {
  run mono ../../bin/Debug/Compat.exe projects/PInvokeExample/PInvokeExample/bin/Debug/PInvokeExample.exe
  echo "$output"
  [ "$status" -eq 0 ]
  echo "$output" | grep "P System.Int32 PlatformInvokeTest::puts(System.String) < msvcrt.dll"
  echo "$output" | grep "P System.Int32 PlatformInvokeTest::_flushall() < msvcrt.dll"
}

@test "should treat pinvokes as failures if --treat-pinvoke-as-error flag provided" {
  run mono ../../bin/Debug/Compat.exe --treat-pinvoke-as-error projects/PInvokeExample/PInvokeExample/bin/Debug/PInvokeExample.exe
  echo "$output"
  [ "$status" -eq 113 ]
}

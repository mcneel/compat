#!/usr/bin/env bats

function setup {
  cd ${COMPAT_ROOT}/test/integration
  xbuild projects/PInvokeExample/PInvokeExample.sln
}

@test "run compat against pinvoke example project" {
  run mono ../../bin/Debug/Compat.exe projects/PInvokeExample/PInvokeExample/bin/Debug/PInvokeExample.exe
  echo "$output"
  [ "$status" -eq 0 ]
}

@test "run compat against pinvoke example project with --treat-pinvoke-as-error" {
  run mono ../../bin/Debug/Compat.exe --treat-pinvoke-as-error projects/PInvokeExample/PInvokeExample/bin/Debug/PInvokeExample.exe
  echo "$output"
  [ "$status" -eq 113 ]
}

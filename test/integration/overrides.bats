#!/usr/bin/env bats

function setup {
  cd ${COMPAT_ROOT}/test/integration
  run xbuild projects/rdktest/rdktest.sln
}

@test "test rdktest against the rhinocommon that it was compiled with" {
  run mono ../../bin/Debug/Compat.exe projects/rdktest/rdktest/bin/Debug/rdktest.dll lib/rhino_en-us_6.0.16231.01091/RhinoCommon.dll
  echo "$output"
  [ "$status" -eq 0 ]
}

@test "test rdktest against a rhinocommon that has a different abstract method" {
  run mono ../../bin/Debug/Compat.exe projects/rdktest/rdktest/bin/Debug/rdktest.dll lib/rhino_en-us_6.0.16176.11441/RhinoCommon.dll
  echo "$output"
  [ "$status" -eq 112 ]
}

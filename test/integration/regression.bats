#!/usr/bin/env bats

function setup {
  cd ${COMPAT_ROOT}/test/integration
  msbuild projects/rdktest/rdktest.sln
}

# @test "should pass" {
#   run mono ../../bin/Release/Compat.exe files/SampleCsCommands.rhp lib/rhino_en-us_6.0.17318.12581/RhinoCommon.dll
#   # echo "$output"
#   echo "$output" | grep "✗"
#   [ "$status" -eq 0 ]
# }

@test "should successfully resolve multidimensional arrays" {
  run mono ../../bin/Release/Compat.exe projects/rdktest/2darray_test/bin/Debug/2darray_test.dll lib/rhino_en-us_6.0.17318.12581/RhinoCommon.dll
  echo "$output" | grep "✗" || :
  [ "$status" -eq 0 ]
}

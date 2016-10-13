#!/usr/bin/env bats

@test "should not work with directories" {
  run mono bin/Release/Compat.exe bin/Release
  echo "$output"
  [ "$status" -eq 110 ]
}

@test "should not work with zip archives" {
  run mono bin/Release/Compat.exe test/integration/files/test.zip
  echo "$output"
  [ "$status" -eq 110 ]
}

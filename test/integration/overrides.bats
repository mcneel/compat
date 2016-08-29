#!/usr/bin/env bats

function setup {
  cd ${COMPAT_ROOT}/test/integration
  xbuild projects/rdktest/rdktest.sln
}

@test "should exit with 0 if all abstract methods of the base class are implemented in the derived class" {
  run mono ../../bin/Debug/Compat.exe projects/rdktest/rdktest/bin/Debug/rdktest.dll lib/rhino_en-us_6.0.16231.01091/RhinoCommon.dll
  echo "$output"
  [ "$status" -eq 0 ]
  echo "$output" | grep "Overrides (Rhino.PlugIns.RenderPlugIn)"
  echo "$output" | grep "✓ Rhino.Commands.Result Rhino.PlugIns.RenderPlugIn::Render(Rhino.RhinoDoc,Rhino.Commands.RunMode,System.Boolean) < RhinoCommon.dll"
}

@test "should fail if the signatures of overridden abstract methods have changed" {
  run mono ../../bin/Debug/Compat.exe projects/rdktest/rdktest/bin/Debug/rdktest.dll lib/rhino_en-us_6.0.16176.11441/RhinoCommon.dll
  echo "$output"
  [ "$status" -eq 112 ]
}

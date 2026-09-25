# Incompatible plug-in samples

Rhino 8 plug-ins that fail Rhino 9's compatibility check in different ways (RH-95428).
Build with `dotnet build IncompatiblePlugins.slnx`; they land in `out\<name>\`. Builds against the Rhino 8
NuGet packages (pass `-p:Rhino8Version=...` to pick another).

The build also puts a package for each one in `packages\`. Add that folder as a source in Rhino's
Package Manager options, search for `rh95428`, install them, and restart Rhino to load them all.

| File | Expected in Rhino 9 |
| --- | --- |
| `RhinoApiPlugin.rhp` | Won't load: uses Rhino features that have changed. |
| `NetFrameworkPlugin.rhp` | .NET Core on Windows: won't load, with the .NET Framework dialog. .NET Framework (`/netfx`): loads. Mac: won't load, no `SetDotNetRuntime` advice. |
| `BinaryFormatterPlugin.rhp` | Rhino: loads. Rhino.Inside without BinaryFormatter enabled: won't load, uses a .NET feature the app turned off. |
| `CompatiblePlugin.rhp` | Loads with no message; run `CompatibleTest` to check it. |
| `RhinoApiComponents.gha` | Warns it may not work (Rhino features changed), then loads. |
| `NetFrameworkComponents.gha` | .NET Core: warns it may not work (.NET Framework), then loads; the component is under Params > Util. |
| `CompatibleComponents.gha` | Loads with no message; the component is under Params > Util. |

Every `.rhp` has all the developer contact details, to check the vendor section of the load error and
plug-in properties dialogs. `NetFrameworkPlugin.rhp` writes its web sites without `https://`, as vendors often do.

Results are cached per file, so delete the `compat_cache*` folders in Rhino's local data folder (under `%LOCALAPPDATA%\McNeel\Rhinoceros` on Windows) to see a first-run
check again.

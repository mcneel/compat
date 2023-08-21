using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Compat
{
  partial class Program
  {
    static Logger logger = new Logger() { Level = Logger.LogLevel.INFO };

    static readonly string version = Properties.Resources.Version.TrimEnd(System.Environment.NewLine.ToCharArray());

    // error codes
    public const int ERROR_UNHANDLED_EXCEPTION = 128; // git uses 128 a lot, so why not
    public const int ERROR_BAD_COMMAND = 100;
    public const int ERROR_NOT_DOTNET = 110;
    public const int ERROR_NOT_THERE = 111;
    public const int ERROR_COMPAT = 112;
    public const int ERROR_PINVOKE = 113;
    public const int ERROR_WARNING = 114;

    static bool quiet = false;
    static bool checkAccess = false;
    static IDictionary<string, AssemblyDefinition> cache;

    static void Usage(string message)
    {
      Console.WriteLine("compat/{0}\nUsage: Compat [-q | --quiet | --debug] [--treat-pinvoke-as-error] [--check-access] [--check-system-assemblies] <assembly> <reference>...", version);
      if (message != null) logger.Warning(message);
    }

    internal static int Main(string[] args)
    {
      if (args.Length < 1)
      {
        Usage("Not enough arguments");
        return ERROR_BAD_COMMAND;
      }

      // control verbosity
      
      // reset variables for re-entry
      quiet = false;
      logger.Level = Logger.LogLevel.INFO;
      checkAccess = false;
      
      if (args[0] == "--quiet" || args[0] == "-q")
      {
        quiet = true;
        logger.Level = Logger.LogLevel.WARNING;
        args = args.Skip(1).ToArray();
      }
      else if (args[0] == "--debug")
      {
        logger.Level = Logger.LogLevel.DEBUG;
        args = args.Skip(1).ToArray();
      }

      // again, check if we have enough arguments
      if (args.Length < 1)
      {
        Usage("Not enough arguments");
        return ERROR_BAD_COMMAND;
      }

      // should we return an error code if pinvokes exist?

      bool treatPInvokeAsError = false;
      if (args[0] == "--treat-pinvoke-as-error")
      {
        treatPInvokeAsError = true;
        args = args.Skip(1).ToArray();
      }

      if (args[0] == "--check-access")
      {
        checkAccess = true;
        args = args.Skip(1).ToArray();
      }

      if (args[0] == "--check-system-assemblies")
      {
        args = args.Skip(1).ToArray();

        var systemAssemblies = new[]
        {
          Assembly.Load(new AssemblyName("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")).Location,
          Assembly.Load(new AssemblyName("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")).Location,
          Assembly.Load(new AssemblyName("System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")).Location,
          typeof(System.Windows.Forms.Appearance).Assembly.Location
        };

        args = args.Concat(systemAssemblies).ToArray();
      }

      // again, check if we have enough arguments
      if (args.Length < 1)
      {
        Usage("Not enough arguments");
        return ERROR_BAD_COMMAND;
      }

      // first arg is the path to the main assembly being processed
      string fileName = args[0];

      if (!File.Exists(fileName))
      {
        // if the file doesn't exist, it might be a directory
        // TODO: handle directories
        if (Directory.Exists(fileName))
        {
          logger.Error("{0} appears to be a directory; .NET assemblies only, please.", fileName);
          return ERROR_NOT_DOTNET;
        }
        logger.Error("Couldn't find {0}. Are you sure it exists?", fileName);
        return ERROR_NOT_THERE;
      }

      // check that the main file is a dot net assembly
      // this gives a clearer error message than the "one or more..." error
      System.Reflection.AssemblyName assemblyName;
      try
      {
        assemblyName = System.Reflection.AssemblyName.GetAssemblyName(fileName);
      }
      catch (System.BadImageFormatException)
      {
        logger.Error("{0} is not a .NET assembly.", fileName);
        return ERROR_NOT_DOTNET;
      }

      var token = assemblyName.GetPublicKeyToken();
      bool isIgnoreAssembly = false;
      if (token != null)
      {
        var tokenName = GetPublicKeyTokenName(token);
        if (NetCore.IgnorePublicKeys.Contains(tokenName))
          isIgnoreAssembly = true;

        if (NetCore.IgnoreAssemblies.Contains((assemblyName.Name, tokenName)))
          isIgnoreAssembly = true;

        if (NetCore.RunningInNetCore && NetCore.IgnoreNetCoreAssemblies.Contains((assemblyName.Name, tokenName)))
          isIgnoreAssembly = true;
      }

      // load module and assembly resolver
      ModuleDefinition module;
      CustomAssemblyResolver customResolver;
      try
      {
        // second arg and onwards should be paths to reference assemblies
        // instantiate custom assembly resolver that loads reference assemblies into cache
        // note: ONLY these assemblies will be available to the resolver
        customResolver = new CustomAssemblyResolver(args.Skip(1));

        // load the plugin module (with the custom assembly resolver)
        // TODO: perhaps we should load the plugin assembly then iterate through all modules
        module = ModuleDefinition.ReadModule(fileName, new ReaderParameters
        {
          AssemblyResolver = customResolver
        });
      }
      catch (BadImageFormatException)
      {
        logger.Error("One (or more) of the files specified is not a .NET assembly");
        return ERROR_NOT_DOTNET;
      }
      catch (FileNotFoundException e)
      {
        logger.Error("Couldn't find {0}. Are you sure it exists?", e.FileName);
        return ERROR_NOT_THERE;
      }

      if (module.Assembly.Name.Name == "")
      {
        logger.Error("Assembly has no name. This is unexpected.");
        return ERROR_UNHANDLED_EXCEPTION;
      }

      // extract cached reference assemblies from custom assembly resolver
      // we'll query these later to make sure we only attempt to resolve a reference when the
      // definition is defined in an assembly in this list
      cache = customResolver.Cache;

      // print assembly name
      logger.Warning("{0}\n", module.Assembly.FullName);

      Version rhinoCommonVersion = null;
      Version rhinoDotNetVersion = null;

      // print assembly references (buildtime)
      if (module.AssemblyReferences.Count > 0)
      {
        logger.Info("Assembly references:", module.Assembly.Name.Name);
        foreach (AssemblyNameReference reference in module.AssemblyReferences)
        {
          logger.Info("  {0}", reference.FullName);
          if (reference.Name == "RhinoCommon" && GetPublicKeyTokenName(reference.PublicKeyToken) == RhinoPublicKey)
          {
            rhinoCommonVersion = reference.Version;
          }
          if (reference.Name == "Rhino_DotNet" && GetPublicKeyTokenName(reference.PublicKeyToken) == RhinoPublicKey)
          {
            rhinoDotNetVersion = reference.Version;
          }
        }
      }
      logger.Info("");

      // print cached assembly names (i.e. runtime references)
      if (args.Length > 1)
      {
        logger.Info("Cached assemblies:");
        foreach (var assembly in args.Skip(1))
        {
          logger.Info("  {0}", AssemblyDefinition.ReadAssembly(assembly).FullName);
        }
      }
      else // no reference assemblies. Grab the skipping rope
      {
        logger.Warning("Empty resolution cache (no reference assemblies specified)");
      }
      logger.Info("");

      // mixed-mode?
      bool isMixed = (module.Attributes & ModuleAttributes.ILOnly) != ModuleAttributes.ILOnly;
      logger.Info("Mixed-mode? {0}\n", isMixed);


      // RH-34686: If a mixed-mode assembly, fail if specific types exist as it likely references an old C++ sdk
      // wfcook RH-60405 14-Sep-2020: If plugin version >= 6 we don't need to check because we 
      // haven't broken the sdk.
      string[] invalidTypes = null;
      if (isMixed && !(rhinoCommonVersion?.Major >= 6 || rhinoDotNetVersion?.Major >= 6))
      {
        // if we find any classes with the following prefixes, odds are the assembly references the rhino c++ sdk
        // we can't check for compatibility with the c++ sdk so the plug-in fails the compatibility check
        invalidTypes = new [] { "CRhino", "ON_" };
      }

      // global failure/pinvoke trackers for setting return code
      bool failure = false;
      bool warning = false;
      bool pinvoke = false;

      List<TypeDefinition> types = GetAllTypesAndNestedTypes(module.Types);

      try
      {
        // iterate over all the TYPES
        foreach (TypeDefinition type in types)
        {
          if (!quiet)
            Pretty.Class(type.FullName);

          if (invalidTypes != null)
          {
            foreach (var prefix in invalidTypes)
            {
              if (type.FullName.StartsWith(prefix, StringComparison.Ordinal))
              {
                // fail!
                Pretty.WriteStatus(ResolutionStatus.Failure, $"{type.FullName} is using an obsolete C++ SDK");
                failure = true;
              }
            }
          }

          // iterate over all the METHODS that have a method body
          foreach (MethodDefinition method in type.Methods)
          {
            if (!quiet)
              Pretty.Method(method.FullName);
              
            if (!method.HasBody) // skip if no body
              continue;

            // iterate over all the INSTRUCTIONS
            foreach (var instruction in method.Body.Instructions)
            {
              // skip if no operand
              if (instruction.Operand == null)
                continue;

              logger.Debug(
                  "Found instruction at {0} with code: {1}",
                  instruction.Offset,
                  instruction.OpCode.Code);

              string instructionString = instruction.Operand.ToString() // for sake of consistency
                  .Replace("{", "{{").Replace("}", "}}"); // escape curly brackets

              // get the scope (the name of the assembly in which the operand is defined)
              IMetadataScope scope = GetOperandScope(instruction.Operand);
              if (scope != null)
              {
                // pinvoke?
                ModuleReference nativeModule;
                bool isPInvoke = IsPInvoke(instruction.Operand, out nativeModule);
                if (isPInvoke && nativeModule != null)
                {
                  if (treatPInvokeAsError || !quiet)
                    Pretty.Instruction(ResolutionStatus.PInvoke, nativeModule.Name, instructionString);
                  pinvoke = true;
                  continue;
                }
                
                // is it a .NET Core API pass its specific status.
                if (NetCore.NetCoreExceptionApis.TryGetValue(scope.Name, out var apis) && apis.TryGetValue(instructionString, out var status))
                {
                  Pretty.Instruction(status, scope.Name, instructionString);

                  if (isIgnoreAssembly && status == ResolutionStatus.Failure)
                    status = ResolutionStatus.Warning;

                  failure |= status == ResolutionStatus.Failure;
                  warning |= status == ResolutionStatus.Warning;
                  continue;
                }

                // skip if scope is not in the list of cached reference assemblies
                if (!cache.ContainsKey(scope.Name))
                {
                  Pretty.Instruction(ResolutionStatus.Skipped, scope.Name, instructionString);
                  continue;
                }

                logger.Debug("{0} is on the list so let's try to resolve it", scope.Name);
                logger.Debug(instruction.Operand.ToString());
                // try to resolve operand
                // this is the big question - does the field/method/class exist in one of
                // the cached reference assemblies
                bool success = TryResolve(instruction.Operand, type);
                if (success || CheckMultidimensionalArray(instruction, method, type, scope))
                {
                  Pretty.Instruction(ResolutionStatus.Success, scope.Name, instructionString);
                }
                else if (isIgnoreAssembly)
                {
                  // if assembly is ignored, report warnings only
                  Pretty.Instruction(ResolutionStatus.Warning, scope.Name, instructionString);
                  warning = true; // set global failure (non-zero exit code)
                }
                else
                {
                  Pretty.Instruction(ResolutionStatus.Failure, scope.Name, instructionString);
                  failure = true; // set global failure (non-zero exit code)
                }
              }
            }
            // check that all abstract methods in the base type (where appropriate) have been implemented
            // note: base type resolved against the referenced assemblies
            failure |= CheckAbstractMethods(type) == false;
          }
        }
      }
      catch (Exception ex)
      {
        logger.Warning($"Exception occurred, cannot check the rest of the module\n{ex}");
        // we don't do anything here, all we're doing is skipping the check if .Net can't look at the methods.
        // BobCAD has this issue.
      }

      // exit code
      if (failure)
        return ERROR_COMPAT;
      if (pinvoke && treatPInvokeAsError)
        return ERROR_PINVOKE;
      if (warning)
        return ERROR_WARNING;

      return 0; // a-ok
    }

    internal static string GetPublicKeyTokenName(byte[] token)
    {
      return token?.Aggregate(string.Empty, (s, b) => s += b.ToString("x2", CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Gets all types and nested types recursively.
    /// </summary>
    /// <param name="types">A bunch of types.</param>
    /// <returns>All the types and their nested types (and their nested types...).</returns>
    static List<TypeDefinition> GetAllTypesAndNestedTypes(IEnumerable<TypeDefinition> types)
    {
      var list = new List<TypeDefinition>();
      foreach (TypeDefinition type in types)
      {
        list.Add(type);
        if (type.HasNestedTypes)
        {
          list.AddRange(GetAllTypesAndNestedTypes(type.NestedTypes)); // recursive!
        }
      }
      return list;
    }

    /// <summary>
    /// Attempt to get the scope if an operand is either a field, a method or a class.
    /// </summary>
    /// <param name="operand">The operand in question.</param>
    /// <returns>The scope, otherwise null.</returns>
    static IMetadataScope GetOperandScope(object operand)
    {
      IMetadataScope scope = null;

      // try to cast operand to either field, method or class
      var mref = operand as MethodReference;
      if (mref != null)
      {
        var declaring_type = mref.DeclaringType;
        if (declaring_type != null)
          scope = declaring_type.Scope;
      }
      else
      {
        var fref = operand as FieldReference;
        if (fref != null)
        {
          var declaring_type = fref.DeclaringType;
          if (declaring_type != null)
            scope = declaring_type.Scope;
        }
        else
        {
          var tref = operand as TypeReference;
          if (tref != null)
          {
            scope = tref.Scope;
          }
        }
      }

      // log output
      if (scope != null)
      {
        logger.Debug("Scope is {0}", scope);
      }

      return scope;
    }

    /// <summary>
    /// Checks if the operand calls a native library, via PInvoke.
    /// </summary>
    /// <param name="operand">The operand in question.</param>
    /// <returns>True if the operand is a PInvoke, otherwise false.</returns>
    static bool IsPInvoke(object operand, out ModuleReference nativeLib)
    {
      nativeLib = null;
      // try to cast operand to method definition and check for PInvoke
      var mdef = operand as MethodDefinition;
      if (mdef != null)
      {
        if (mdef.IsPInvokeImpl)
        {
          logger.Debug("Is PInvoke? {0}", true);
          if (mdef.PInvokeInfo != null)
          {
            nativeLib = mdef.PInvokeInfo.Module;
            logger.Debug("Native library: {0}", nativeLib.Name);
          }
          return true;
        }
      }

      return false;
    }

    /// <summary>
    /// Try to resolve an operand if it is either a field, a method or a class.
    /// </summary>
    /// <param name="operand">The operand in question.</param>
    /// <param name="calling_type">The <see cref="TypeDefinition"/> to which
    /// the <paramref name="operand"/> belongs.</param>
    /// <returns>True if successful.</returns>
    /// <remarks>
    /// Also checks the accessiblity of the resolved member, in case the
    /// modifiers have changed.
    /// </remarks>
    static bool TryResolve(object operand, TypeDefinition calling_type)
    {
      try
      {
        // TODO: why am I casting again?? merge with GetOperandScope perhaps?
        // UPDATE: I tried but it's not as straightforward as first thought!
        var fref = operand as FieldReference;
        if (fref != null)
        {
          var fdef = fref.Resolve();
          if (checkAccess)
            return Utils.IsFieldAccessible(fdef, calling_type);
          return fdef != null;
        }
        var mref = operand as MethodReference;
        if (mref != null)
        {
          var mdef = mref.Resolve();
          if (checkAccess)
            return Utils.IsMethodAccessible(mdef, calling_type);
          return mdef != null;
        }
        var tref = operand as TypeReference;
        if (tref != null)
        {
          var tdef = tref.Resolve();
          if (checkAccess)
            return Utils.IsTypeAccessible(tdef, calling_type);
          return tdef != null;
        }
      }
      catch (AssemblyResolutionException)
      {
        return false;
      }
      return false; // just in case
    }

    static Dictionary<Regex, Mono.Cecil.Cil.OpCode> Patterns = new Dictionary<Regex, Mono.Cecil.Cil.OpCode>()
    {
      { new Regex(@"([a-z0-9\.]+)\[.*?,.*?\]::\.ctor", RegexOptions.IgnoreCase | RegexOptions.Compiled), Mono.Cecil.Cil.OpCodes.Newarr },
      { new Regex(@"([a-z0-9\.]+)\[.*?,.*?\]::Get", RegexOptions.IgnoreCase | RegexOptions.Compiled), Mono.Cecil.Cil.OpCodes.Stelem_Any },
      { new Regex(@"([a-z0-9\.]+)\[.*?,.*?\]::Set", RegexOptions.IgnoreCase | RegexOptions.Compiled), Mono.Cecil.Cil.OpCodes.Ldelem_Any },
      // when you retrieve a struct from an array and immediately call a method on it => Ldelema instead of Ldelem_Any
      { new Regex(@"([a-z0-9\.]+)\[.*?,.*?\]::Address", RegexOptions.IgnoreCase | RegexOptions.Compiled), Mono.Cecil.Cil.OpCodes.Ldelema }
    };

    /// <summary>
    /// Checks to see if the instruction includes a multidimensional array and
    /// if so attempts to reconstruct the instruction as a normal (1d) array
    /// operation (.ctor, getter or setter) and resolve that instead.
    /// </summary>
    /// <returns><c>true</c>, if multidimensional array was successfully
    /// reconstructed as a 1d array instruction and successfully resolved,
    /// <c>false</c> otherwise.</returns>
    /// <param name="instruction">An instruction.</param>
    /// <param name="method">The method which contains the instruction.</param>
    /// <param name="type">The type which contains the method and instruction.</param>
    /// <param name="scope">The scope of the instruction (name of assembly).</param>
    /// <remarks>
    /// Multidimensional array instructions won't resolve because "there's
    /// nothing in the metadata to resolve to: those methods are created on the
    /// fly by the runtime".
    /// See https://www.mail-archive.com/mono-cecil@googlegroups.com/msg03876.html.
    /// </remarks>
    static bool CheckMultidimensionalArray(Mono.Cecil.Cil.Instruction instruction, MethodDefinition method, TypeDefinition type, IMetadataScope scope)
    {
      var methodInfo = instruction.Operand as MemberReference;
      
      // if it's not an array we don't need to resolve this here
      if (methodInfo != null && methodInfo.DeclaringType != null && !methodInfo.DeclaringType.IsArray)
        return false;

      // ensure the declaring type and the element type are resolved and we should be good here.
      // the below was taking out the element type via regex but we have apis to get it instead
      return methodInfo?.DeclaringType?.GetElementType()?.Resolve() != null;

      // this fails with system types when referencing System assembly.. e.g. new string[10,10];
      // var processor = method.Body.GetILProcessor();
      // foreach (var pattern in Patterns)
      // {
      //   var m = pattern.Key.Match(instruction.Operand.ToString());
      //   if (m.Success)
      //   {
      //     string full_name = m.Groups[1].Value;
      //     logger.Debug("Attemping to reconstruct multidimensional array instruction as '{0}' with opcode '{1}'", full_name, pattern.Value.Code);
      //     var asm = cache[scope.Name];
      //     var tmp_type = methodInfo?.DeclaringType.GetElementType() ?? asm.MainModule.GetType(full_name);
      //     if (tmp_type == null)
      //     {
      //       logger.Debug("{0} not found in {1}", full_name, scope.Name);
      //       return false;
      //     }
      //     var new_instr = processor.Create(pattern.Value, tmp_type);
      //     return TryResolve(new_instr.Operand, type);
      //   }
      // }

      // return false;
    }

    /// <summary>
    /// Checks that all abstract methods and properties of the base class have been implemented,
    /// as they exist currently in the target assembly.
    /// </summary>
    /// <returns><c>false</c>, if one of more abstract methods were not implemented in the derived class,
    /// <c>true</c> otherwise.</returns>
    /// <param name="type">A class.</param>
    /// <remarks>Resolves the base class against the reference assemblies supplied on the command line.</remarks>
    static bool CheckAbstractMethods(TypeDefinition type)
    {
      bool failure = false;
      // ensure all abstract methods in the base class are overridden
      TypeDefinition @base = null;

      // 14 Jan 2017 S. Baer
      // If the type itself is abstract, then it doesn't need to implement all
      // of the abstract members in it's base class
      if (null != type.BaseType && !type.IsAbstract)
      {
        // resolve the base class so we're checking against the version of the library that we want
        try
        {
          @base = type.BaseType.Resolve();
        }
        catch (AssemblyResolutionException)
        {
          logger.Info("Couldn't resolve base class: {0}", type.BaseType.FullName);
        }

        if (null != @base)
        {
          // skip if base class isn't defined in one of the reference assemblies
          var scope = @base.Module.Assembly.Name; // be consistent
          if (!cache.ContainsKey(scope.Name))
            return true;

          if (!quiet)
            Console.WriteLine("  Overrides ({0})", @base.FullName);

          foreach (var method in @base.Methods)
          {
            if (!method.IsAbstract)
              continue;

            bool is_overridden = null != Utils.TryMatchMethod(type, method);

            if (is_overridden)
              Pretty.Instruction(ResolutionStatus.Success, scope.Name, method.FullName);
            else
            {
              failure = true;
              Pretty.Instruction(ResolutionStatus.Failure, scope.Name, method.FullName);
            }
          }
        }
      }

      return (!failure);
    }

    public enum ResolutionStatus
    {
      Success,
      Failure,
      Skipped,
      PInvoke,
      Warning
    }
  }
}

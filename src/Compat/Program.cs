﻿using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Compat
{
  class Program
  {
    static Logger logger = new Logger() { Level = Logger.LogLevel.INFO };

    static readonly string version = Properties.Resources.Version.TrimEnd(System.Environment.NewLine.ToCharArray());

    // error codes
    private const int ERROR_UNHANDLED_EXCEPTION = 128; // git uses 128 a lot, so why not
    private const int ERROR_BAD_COMMAND = 100;
    private const int ERROR_NOT_DOTNET = 110;
    private const int ERROR_NOT_THERE = 111;
    private const int ERROR_COMPAT = 112;
    private const int ERROR_PINVOKE = 113;

    static bool quiet = false;
    static IDictionary<string, AssemblyDefinition> cache;

    static void Usage(string message)
    {
      Console.WriteLine("compat/{0}\nUsage: [mono] Compat.exe [-q | --quiet | --debug] [--treat-pinvoke-as-error] <assembly> <reference>...", version);
      if (message != null) logger.Warning(message);
    }

    static int Main(string[] args)
    {
      if (args.Length < 1)
      {
        Usage("Not enough arguments");
        return ERROR_BAD_COMMAND;
      }

      // control verbosity
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
      try
      {
        System.Reflection.AssemblyName.GetAssemblyName(fileName);
      }
      catch (System.BadImageFormatException)
      {
        logger.Error("{0} is not a .NET assembly.", fileName);
        return ERROR_NOT_DOTNET;
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
      logger.Info("{0}\n", module.Assembly.FullName);

      // print assembly references (buildtime)
      if (module.AssemblyReferences.Count > 0)
      {
        logger.Info("Assembly references:", module.Assembly.Name.Name);
        foreach (AssemblyNameReference reference in module.AssemblyReferences)
        {
          logger.Info("  {0}", reference.FullName);
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

      // global failure/pinvoke trackers for setting return code
      bool failure = false;
      bool pinvoke = false;

      List<TypeDefinition> types = GetAllTypesAndNestedTypes(module.Types);

      // iterate over all the TYPES
      foreach (TypeDefinition type in types)
      {
        Pretty.Class("{0}", type.FullName);

        // iterate over all the METHODS that have a method body
        foreach (MethodDefinition method in type.Methods)
        {
          Pretty.Method("{0}", method.FullName);
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
                Pretty.Instruction(ResolutionStatus.PInvoke, nativeModule.Name, instructionString);
                pinvoke = true;
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
              else
              {
                Pretty.Instruction(ResolutionStatus.Failure, scope.Name, instructionString);
                failure = true; // set global failure (non-zero exit code)
              }
            }
          }
        }

        // check that all abstract methods in the base type (where appropriate) have been implemented
        // note: base type resolved against the referenced assemblies
        failure |= CheckAbstractMethods(type) == false;
      }

      // exit code
      if (failure)
        return ERROR_COMPAT;
      if (pinvoke && treatPInvokeAsError)
        return ERROR_PINVOKE;

      return 0; // a-ok
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
          return Utils.IsFieldAccessible(fdef, calling_type);
        }
        var mref = operand as MethodReference;
        if (mref != null)
        {
          var mdef = mref.Resolve();
          return Utils.IsMethodAccessible(mdef, calling_type);
        }
        var tref = operand as TypeReference;
        if (tref != null)
        {
          var tdef = tref.Resolve();
          return Utils.IsTypeAccessible(tdef, calling_type);
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
      var processor = method.Body.GetILProcessor();
      foreach (var pattern in Patterns)
      {
        var m = pattern.Key.Match(instruction.Operand.ToString());
        if (m.Success)
        {
          string full_name = m.Groups[1].Value;
          logger.Debug("Attemping to reconstruct multidimensional array instruction as '{0}' with opcode '{1}'", full_name, pattern.Value.Code);
          var asm = cache[scope.Name];
          var tmp_type = asm.MainModule.GetType(full_name);
          if (tmp_type == null)
          {
            logger.Debug("{0} not found in {1}", full_name, scope.Name);
            return false;
          }
          var new_instr = processor.Create(pattern.Value, tmp_type);
          return TryResolve(new_instr.Operand, type);
        }
      }

      return false;
    }

    /// <summary>
    /// Custom assembly resolver to load specified reference assemblies into the cache.
    /// Imitates DefaultAssemblyResolver except or the version agnostic cache.
    /// </summary>
    class CustomAssemblyResolver : BaseAssemblyResolver
    {
      readonly IDictionary<string, AssemblyDefinition> cache;

      /// <summary>
      /// Creates a Custom Assembly Resolver and preload the cache with some reference assemblies.
      /// </summary>
      /// <param name="paths">Paths to reference assemblies.</param>
      public CustomAssemblyResolver(IEnumerable<string> paths)
      {
        cache = new Dictionary<string, AssemblyDefinition>(StringComparer.Ordinal);
        foreach (string path in paths)
        {
          var assembly = AssemblyDefinition.ReadAssembly(path);
          this.RegisterAssembly(assembly);
        }
      }

      public override AssemblyDefinition Resolve(AssemblyNameReference name)
      {
        if (name == null)
          throw new ArgumentNullException("name");

        AssemblyDefinition assembly;
        logger.Debug("Searching cache for {0}", name.Name);
        if (cache.TryGetValue(name.Name, out assembly)) // use Name instead of FullName (see below)
        {
          logger.Debug("Found {0}", assembly.FullName);
          return assembly;
        }
        logger.Debug("We don't care about {0}", name.Name);
        // if it's not in the cache, it's not important!
        throw new AssemblyResolutionException(name);
      }

      protected void RegisterAssembly(AssemblyDefinition assembly)
      {
        if (assembly == null)
          throw new ArgumentNullException("assembly");

        // Store assembly in cache in version agnostic way
        // FullName = "RhinoCommon, Version=5.1.30000.16, Culture=neutral, PublicKeyToken=552281e97c755530"
        // Name     = "RhinoCommon"
        var name = assembly.Name.Name; // use Name as key so that versions don't matter
        if (cache.ContainsKey(name))
          return; // TODO: throw an error here and tell the user what's wrong

        cache[name] = assembly;
      }

      /// <summary>
      /// Gets names of assemblies loaded into resolver cache. This array is a copy (paranoid or what?!).
      /// </summary>
      public IDictionary<string, AssemblyDefinition> Cache
      {
        get { return new Dictionary<string, AssemblyDefinition>(cache); }
      }
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
          logger.Warning("Couldn't resolve base class: {0}", type.BaseType.FullName);
        }

        if (null != @base)
        {
          // skip if base class isn't defined in one of the reference assemblies
          var scope = @base.Module.Assembly.Name; // be consistent
          if (!cache.ContainsKey(scope.Name))
            return true;

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

    class Logger
    {
      public enum LogLevel
      {
        ERROR,
        WARNING,
        INFO,
        DEBUG
      }

      public LogLevel Level = LogLevel.DEBUG;

      public void Debug(string format, params object[] args)
      {
        if (Level >= LogLevel.DEBUG)
          Console.WriteLine("DEBUG " + format, args);
      }

      public void Info(string format, params object[] args)
      {
        if (Level >= LogLevel.INFO)
          Console.WriteLine(format, args);
      }

      public void Warning(string format, params object[] args)
      {
        if (Level >= LogLevel.WARNING)
          WriteLine(string.Format(format, args), ConsoleColor.DarkYellow);
      }

      public void Error(string format, params object[] args)
      {
        if (Level >= LogLevel.ERROR)
          WriteLine(string.Format(format, args), ConsoleColor.Red);
      }

      void WriteLine(string message, ConsoleColor color)
      {
        Console.ForegroundColor = color;
        try
        {
          Console.WriteLine(message);
        }
        finally
        {
          Console.ResetColor();
        }
      }
    }

    enum ResolutionStatus
    {
      Success,
      Failure,
      Skipped,
      PInvoke
    }

    static class Pretty
    {
      static public void Class(string format, params object[] args)
      {
        WriteColor(format, args, ConsoleColor.Magenta);
      }

      static public void Method(string format, params object[] args)
      {
        WriteColor("  " + format, args, ConsoleColor.DarkCyan);
      }

      static public void Instruction(ResolutionStatus status, string scope, string format, params object[] args)
      {
        string indent = "    ";
        format += " < " + scope;
        if (status == ResolutionStatus.Success)
        {
          if (!quiet)
            WriteColor(indent + "\u2713 " + format, args, ConsoleColor.Green);
        }
        else if (status == ResolutionStatus.Failure)
          WriteColor(indent + "\u2717 " + format, args, ConsoleColor.Red);
        else if (status == ResolutionStatus.PInvoke)
          WriteColor(indent + "P " + format, args, ConsoleColor.DarkYellow);
        else // skipped
        {
          if (!quiet)
            WriteColor(indent + "\u271D " + format, args, ConsoleColor.Gray);
        }
      }

      static void WriteColor(string format, object[] args, ConsoleColor color)
      {
        Console.ForegroundColor = color;
        try
        {
          Console.WriteLine(format, args);
        }
        finally
        {
          Console.ResetColor();
        }
      }
    }
  }
}

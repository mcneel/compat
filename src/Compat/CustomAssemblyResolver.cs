using Mono.Cecil;
using System;
using System.Collections.Generic;

namespace Compat
{
  partial class Program
  {
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
  }
}

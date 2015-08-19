using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compat
{
    class Program
    {
        static int Main(string[] args)
        {
            // first arg is the path to the main assembly being processed
            if (args.Length < 1)
            {
                return 100; // not enough arguments?
            }
            string fileName = args[0];

            // second arg and onwards should be paths to reference assemblies
            // instantiate custom assembly resolver that loads reference assemblies into cache
            IAssemblyResolver customResolver = new CustomAssemblyResolver(args.Skip(1));

            // load the plugin module (with the custom assembly resolver)
            // TODO: perhaps we should load the plugin assembly then iterate through all modules
            ModuleDefinition module = ModuleDefinition.ReadModule(fileName, new ReaderParameters
            {
                AssemblyResolver = customResolver
            });

            // extract cached reference assemblies from custom assembly resolver
            // we'll query these later to make sure we only attempt to resolve a reference when the
            // definition is defined in an assembly in this list
            AssemblyDefinition[] cache = (customResolver as CustomAssemblyResolver).Assemblies;

            // print assembly name, cached assembly names and reference assembly names
            Console.WriteLine("{0}\n", module.Assembly.FullName);
            if (args.Length > 1)
            {
                foreach (var assembly in args.Skip(1))
                {
                    Console.WriteLine(AssemblyDefinition.ReadAssembly(assembly).FullName);
                }
                Console.WriteLine("");
            }
            foreach (AssemblyNameReference reference in module.AssemblyReferences)
            {
                Console.WriteLine(reference.FullName);
            }
            Console.WriteLine("");

            // global failure tracker
            bool failure = false;

            // iterate over all the TYPES
            foreach (TypeDefinition type in module.Types)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("CLASS\t{0}", type.FullName);
                Console.ResetColor();

                // iterate over all the METHODS that have a method body
                foreach (MethodDefinition method in type.Methods)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("METHOD\t{0}", method.FullName);
                    Console.ResetColor();
                    if (!method.HasBody) // skip if no body
                        continue;
                    
                    // iterate over all the INSTRUCTIONS
                    foreach (var instruction in method.Body.Instructions)
                    {
                        Console.WriteLine(
                            "{0}\t{1}\t{2}",
                            instruction.Offset,
                            instruction.OpCode.Code,
                            instruction.Operand == null ? "<null>" : string.Format("{0} / {1}", instruction.Operand.GetType().FullName, instruction.Operand.ToString()));

                        // get the scope (the name of the assembly in which the operand is defined)
                        IMetadataScope scope = GetOperandScope(instruction.Operand);
                        if (scope != null)
                        {
                            // skip if scope is not in the list of cached reference assemblies
                            if (!cache.Any(a => a.Name.Name == scope.Name))
                            {
                                Console.WriteLine("RESULT\tSkip ({0} is not in the list)", scope.Name);
                                continue;
                            }
                            // try to resolve operand
                            // this is the big question - does the field/method/class exist in one of
                            // the cached reference assemblies
                            bool success = TryResolve(instruction.Operand);
                            if (success)
                                // TODO: print nothing (unless debug)
                                Console.ForegroundColor = ConsoleColor.Green;
                            else
                            {
                                // TODO: print information about field/method/class that couldn't be resolved
                                Console.ForegroundColor = ConsoleColor.Red;
                                failure = true; // set global failure (non-zero exit code)
                            }
                            Console.WriteLine("RESULT\t{0}", success); // print resolution status
                            Console.ResetColor();
                        }
                    }
                }
            }

            // exit code
            if (failure)
                return 1;
            else
                return 0;
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
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("SCOPE\t{0}", scope);
                Console.ResetColor();
            }

            return scope;
        }

        /// <summary>
        /// Try to resolve an operand if it is either a field, a method or a class.
        /// </summary>
        /// <param name="operand">The operand in question.</param>
        /// <returns>True if successful.</returns>
        static bool TryResolve(object operand)
        {
            try
            {
                // TODO: why am I casting again?? merge with GetOperandScope perhaps?
                var fref = operand as FieldReference;
                if (fref != null)
                {
                    fref.Resolve();
                    return true;
                }
                var mref = operand as MethodReference;
                if (mref != null)
                {
                    mref.Resolve();
                    return true;
                }
                var tref = operand as TypeReference;
                if (tref != null)
                {
                    tref.Resolve();
                    return true;
                }
            }
            catch (AssemblyResolutionException)
            {
                return false;
            }
            return false; // just in case
        }

        // TODO: investigate http://stackoverflow.com/a/18080305
        /// <summary>
        /// Custom assembly resolver to load specified reference assemblies into the cache.
        /// </summary>
        class CustomAssemblyResolver : DefaultAssemblyResolver
        {
            private List<AssemblyDefinition> _cache; // store cached reference assemblies
            
            /// <summary>
            /// Creates a Custom Assembly Resolver and preload the cache with some reference assemblies.
            /// </summary>
            /// <param name="paths">A bunch of reference assemblies.</param>
            public CustomAssemblyResolver(IEnumerable<string> paths)
                : base()
            {
                this._cache = new List<AssemblyDefinition>();
                foreach (string path in paths)
                {
                    var assembly = AssemblyDefinition.ReadAssembly(path);
                    this.RegisterAssembly(assembly);
                    this._cache.Add(assembly); // store assemblies for lookup later
                }
            }

            /// <summary>
            /// Gets names of assemblies loaded into resolver cache. This array is a copy.
            /// </summary>
            public AssemblyDefinition[] Assemblies
            {
                get { return _cache.ToArray(); } // copied so things can't get messed up
            }
        }
    }
}

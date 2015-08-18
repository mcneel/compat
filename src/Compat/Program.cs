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
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                return;
            }
            string fileName = args[0];
            
            // instantiate custom assembly resolver that loads reference assemblies
            IAssemblyResolver customResolver = new CustomAssemblyResolver(args.Skip(1));

            // load the plugin module
            // TODO: perhaps we should load the plugin assembly then iterate through all modules
            ModuleDefinition module = ModuleDefinition.ReadModule(fileName, new ReaderParameters
            {
                AssemblyResolver = customResolver
            });
            foreach (AssemblyNameReference reference in module.AssemblyReferences)
            {
                Console.WriteLine(reference.FullName);
            }

            // iterate over all the types
            foreach (TypeDefinition type in module.Types)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("CLASS\t{0}", type.FullName);
                Console.ResetColor();

                // iterate over all the methods that have a method body
                foreach (MethodDefinition method in type.Methods)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("METHOD\t{0}", method.FullName);
                    Console.ResetColor();
                    if (!method.HasBody)
                        continue;
                    
                    // iterate over all the instructions
                    foreach (var instruction in method.Body.Instructions)
                    {
                        Console.WriteLine(
                            "{0}\t{1}\t{2}",
                            instruction.Offset,
                            instruction.OpCode.Code,
                            instruction.Operand == null ? "<null>" : string.Format("{0} / {1}", instruction.Operand.GetType().FullName, instruction.Operand.ToString()));

                        // get the scope
                        IMetadataScope scope = GetOperandScope(instruction.Operand);
                        if (scope != null)
                        {
                            // try to resolve operand
                            bool success = TryResolve(instruction.Operand);
                            if (success)
                                Console.ForegroundColor = ConsoleColor.Green;
                            else
                                Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("RESULT\t{0}", success);
                            Console.ResetColor();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Attempt to get the scope if an operand is either a field, a method or a class.
        /// </summary>
        /// <param name="operand">The operand in question.</param>
        /// <returns>The scope, otherwise null.</returns>
        static IMetadataScope GetOperandScope(object operand)
        {
            IMetadataScope scope = null;
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
            catch (AssemblyResolutionException e)
            {
                throw e;
                //return false;
            }
            return false;
        }

        /// <summary>
        /// Custom assembly resolver to load specified reference assemblies into the cache.
        /// </summary>
        class CustomAssemblyResolver : DefaultAssemblyResolver
        {
            public CustomAssemblyResolver(IEnumerable<string> paths)
                : base()
            {
                foreach (string path in paths)
                {
                    var assembly = AssemblyDefinition.ReadAssembly(path);
                    this.RegisterAssembly(assembly);
                }
            }

            new public void RegisterAssembly (AssemblyDefinition assembly)
            {
                base.RegisterAssembly(assembly);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace Compat
{
  partial class Program
  {
    public class NetCore
    {
      static Dictionary<string, Dictionary<string, ResolutionStatus>> _netCoreExceptionApis;
      public static Dictionary<string, Dictionary<string, ResolutionStatus>> NetCoreExceptionApis = _netCoreExceptionApis ?? (_netCoreExceptionApis = GetNetCoreExceptionApis().GroupBy(r => r.assembly).ToDictionary(r => r.Key, r => r.ToDictionary(k => k.api, k => k.status)));

      public static IEnumerable<(string assembly, string api, ResolutionStatus status)> GetNetCoreExceptionApis()
      {
        // These will pass API check but will throw exceptions when called.
        
        // CodeDomProvider
        yield return ("System", "System.Void System.CodeDom.Compiler.CodeDomProvider::GenerateCodeFromMember(System.CodeDom.CodeTypeMember,System.IO.TextWriter,System.CodeDom.Compiler.CodeGeneratorOptions)", ResolutionStatus.Failure);
        yield return ("System", "System.CodeDom.Compiler.CompilerResults System.CodeDom.Compiler.CodeDomProvider::CompileAssemblyFromSource(System.CodeDom.Compiler.CompilerParameters,System.String[])", ResolutionStatus.Failure);
        yield return ("System", "System.CodeDom.Compiler.CompilerResults System.CodeDom.Compiler.CodeDomProvider::CompileAssemblyFromFile(System.CodeDom.Compiler.CompilerParameters,System.String[])", ResolutionStatus.Failure);

        // AppDomain
        yield return ("mscorlib", "System.AppDomain System.AppDomain::CreateDomain(System.String)", ResolutionStatus.Failure);
        yield return ("mscorlib", "System.Int32 System.AppDomain::ExecuteAssembly(System.String)", ResolutionStatus.Failure);
        yield return ("mscorlib", "System.Int32 System.AppDomain::ExecuteAssembly(System.String,System.String[])", ResolutionStatus.Failure);
        yield return ("mscorlib", "System.Int32 System.AppDomain::ExecuteAssembly(System.String,System.String[],System.Byte[],System.Configuration.Assemblies.AssemblyHashAlgorithm)", ResolutionStatus.Failure);
      }
      
      /// <summary>
      /// List of public key tokens for assemblies to only give warnings for
      /// </summary>
      public static readonly HashSet<string> IgnorePublicKeys = new HashSet<string> {
        "552281e97c755530", // All Rhino assemblies
      };

      /// <summary>
      /// List of name/public key token pairs for assemblies to only give warnings for
      /// </summary>
      public static readonly HashSet<(string assembly, string publicKeyToken)> IgnoreAssemblies = new HashSet<(string assembly, string publicKeyToken)> {
        ("Microsoft.Build", "b03f5f7f11d50a3a"),
        ("Microsoft.Build.Utilities.Core", "b03f5f7f11d50a3a"),
        ("EntityFramework" ,"b77a5c561934e089"),
        ("System.Reactive" ,"94bc3704cddfc263"),
        ("Serilog.Sinks.File", "24c2f752a8e58a10"),
        ("log4net", "669e0ddf0bb1aa2a"),
        ("NLog", "5120e14c03d0593c"),
      };
    }
  }
}

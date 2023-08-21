using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Compat
{
  partial class Program
  {
    static string RhinoPublicKey = "552281e97c755530";

    public class NetCore
    {
      static bool? runningInNetCore;
      public static bool RunningInNetCore = runningInNetCore ?? (runningInNetCore = System.Environment.Version.Major >= 5 || RuntimeInformation.FrameworkDescription.StartsWith(".NET Core", StringComparison.OrdinalIgnoreCase)).Value;

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
        RhinoPublicKey, // All Rhino assemblies
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
        ("Renci.SshNet", "1cee9f8bde3db106"),

        // list of assemblies from nuget packages that developers sometimes forget to omit
        ("Grasshopper", "dda4f5ec2cd80803"),
        ("GH_IO", "6a29997d2e6b4f97"),
        ("Rhino.UI", "552281e97c755530"),
        ("RhinoCommon", "552281e97c755530"),
        ("RhinoWindows", "552281e97c755530"),
        ("Ed.Eto", "552281e97c755530"),
        ("Eto", "552281e97c755530"),
        ("Eto.Wpf", "552281e97c755530"),
        ("Eto.macOS", "552281e97c755530")
      };

      public static readonly HashSet<(string assembly, string publicKeyToken)> IgnoreNetCoreAssemblies = new HashSet<(string assembly, string publicKeyToken)> {
        // List of included assemblies in Rhino
        // Use CompatTests.HelperTest.GenerateRhinoAssemblyList to generate this
        ("Alternet.Common.v8", "8032721e70924a63"),
        ("Alternet.Editor.v8", "8032721e70924a63"),
        ("Alternet.Syntax.Parsers.Advanced.v8", "8032721e70924a63"),
        ("Alternet.Syntax.v8", "8032721e70924a63"),
        ("Azure.Core", "92742159e12e44c8"),
        ("Azure.Identity", "92742159e12e44c8"),
        ("IronPython", "7f709c5b713576e1"),
        ("IronPython.Modules", "7f709c5b713576e1"),
        ("IronPython.SQLite", "7f709c5b713576e1"),
        ("IronPython.Wpf", "7f709c5b713576e1"),
        ("Microsoft.Bcl.AsyncInterfaces", "cc7b13ffcd2ddd51"),
        ("Microsoft.Data.SqlClient", "23ec7fc2d6eaa4a5"),
        ("Microsoft.Dynamic", "7f709c5b713576e1"),
        ("Microsoft.Extensions.ObjectPool", "adb9793829ddae60"),
        ("Microsoft.Identity.Client", "0a613f4dd989e8ae"),
        ("Microsoft.Identity.Client.Extensions.Msal", "0a613f4dd989e8ae"),
        ("Microsoft.IdentityModel.JsonWebTokens", "31bf3856ad364e35"),
        ("Microsoft.IdentityModel.Logging", "31bf3856ad364e35"),
        ("Microsoft.IdentityModel.Protocols", "31bf3856ad364e35"),
        ("Microsoft.IdentityModel.Protocols.OpenIdConnect", "31bf3856ad364e35"),
        ("Microsoft.IdentityModel.Tokens", "31bf3856ad364e35"),
        ("Microsoft.Scripting", "7f709c5b713576e1"),
        ("Microsoft.Scripting.Metadata", "7f709c5b713576e1"),
        ("Mono.Cecil", "50cebf1cceb9d05e"),
        ("Mono.Cecil.Mdb", "50cebf1cceb9d05e"),
        ("Mono.Cecil.Pdb", "50cebf1cceb9d05e"),
        ("Mono.Cecil.Rocks", "50cebf1cceb9d05e"),
        ("Mono.Unix", "cc7b13ffcd2ddd51"),
        ("System.ComponentModel.Composition", "b77a5c561934e089"),
        ("System.Data.SqlClient", "b03f5f7f11d50a3a"),
        ("System.IdentityModel.Tokens.Jwt", "31bf3856ad364e35"),
        ("System.IO.Pipelines", "cc7b13ffcd2ddd51"),
        ("System.IO.Ports", "cc7b13ffcd2ddd51"),
        ("System.Management", "b03f5f7f11d50a3a"),
        ("System.Private.ServiceModel", "b03f5f7f11d50a3a"),
        ("System.Runtime.Caching", "b03f5f7f11d50a3a"),
        ("System.ServiceModel", "b77a5c561934e089"),
        ("System.ServiceModel.Http", "b03f5f7f11d50a3a"),
        ("System.ServiceModel.Primitives", "b03f5f7f11d50a3a"),
        ("System.ServiceModel.Syndication", "cc7b13ffcd2ddd51"),
        ("Xfinium.Pdf.NetCore", "3a083ecebc95eb1c"),
    
    
        // Part of the .NET Core runtime
        // Use CompatTests.HelperTests.GenerateSystemAssemblyList test to generate this
        ("Microsoft.CSharp", "b03f5f7f11d50a3a"),
        ("Microsoft.VisualBasic.Core", "b03f5f7f11d50a3a"),
        ("Microsoft.VisualBasic", "b03f5f7f11d50a3a"),
        ("Microsoft.Win32.Primitives", "b03f5f7f11d50a3a"),
        ("Microsoft.Win32.Registry", "b03f5f7f11d50a3a"),
        ("mscorlib", "b77a5c561934e089"),
        ("netstandard", "cc7b13ffcd2ddd51"),
        ("System.AppContext", "b03f5f7f11d50a3a"),
        ("System.Buffers", "cc7b13ffcd2ddd51"),
        ("System.Collections.Concurrent", "b03f5f7f11d50a3a"),
        ("System.Collections", "b03f5f7f11d50a3a"),
        ("System.Collections.Immutable", "b03f5f7f11d50a3a"),
        ("System.Collections.NonGeneric", "b03f5f7f11d50a3a"),
        ("System.Collections.Specialized", "b03f5f7f11d50a3a"),
        ("System.ComponentModel.Annotations", "b03f5f7f11d50a3a"),
        ("System.ComponentModel.DataAnnotations", "31bf3856ad364e35"),
        ("System.ComponentModel", "b03f5f7f11d50a3a"),
        ("System.ComponentModel.EventBasedAsync", "b03f5f7f11d50a3a"),
        ("System.ComponentModel.Primitives", "b03f5f7f11d50a3a"),
        ("System.ComponentModel.TypeConverter", "b03f5f7f11d50a3a"),
        ("System.Configuration", "b03f5f7f11d50a3a"),
        ("System.Console", "b03f5f7f11d50a3a"),
        ("System.Core", "b77a5c561934e089"),
        ("System.Data.Common", "b03f5f7f11d50a3a"),
        ("System.Data.DataSetExtensions", "b77a5c561934e089"),
        ("System.Data", "b77a5c561934e089"),
        ("System.Diagnostics.Contracts", "b03f5f7f11d50a3a"),
        ("System.Diagnostics.Debug", "b03f5f7f11d50a3a"),
        ("System.Diagnostics.DiagnosticSource", "cc7b13ffcd2ddd51"),
        ("System.Diagnostics.FileVersionInfo", "b03f5f7f11d50a3a"),
        ("System.Diagnostics.Process", "b03f5f7f11d50a3a"),
        ("System.Diagnostics.StackTrace", "b03f5f7f11d50a3a"),
        ("System.Diagnostics.TextWriterTraceListener", "b03f5f7f11d50a3a"),
        ("System.Diagnostics.Tools", "b03f5f7f11d50a3a"),
        ("System.Diagnostics.TraceSource", "b03f5f7f11d50a3a"),
        ("System.Diagnostics.Tracing", "b03f5f7f11d50a3a"),
        ("System", "b77a5c561934e089"),
        ("System.Drawing", "b03f5f7f11d50a3a"),
        ("System.Drawing.Primitives", "b03f5f7f11d50a3a"),
        ("System.Dynamic.Runtime", "b03f5f7f11d50a3a"),
        ("System.Formats.Asn1", "cc7b13ffcd2ddd51"),
        ("System.Formats.Tar", "cc7b13ffcd2ddd51"),
        ("System.Globalization.Calendars", "b03f5f7f11d50a3a"),
        ("System.Globalization", "b03f5f7f11d50a3a"),
        ("System.Globalization.Extensions", "b03f5f7f11d50a3a"),
        ("System.IO.Compression.Brotli", "b77a5c561934e089"),
        ("System.IO.Compression", "b77a5c561934e089"),
        ("System.IO.Compression.FileSystem", "b77a5c561934e089"),
        ("System.IO.Compression.ZipFile", "b77a5c561934e089"),
        ("System.IO", "b03f5f7f11d50a3a"),
        ("System.IO.FileSystem.AccessControl", "b03f5f7f11d50a3a"),
        ("System.IO.FileSystem", "b03f5f7f11d50a3a"),
        ("System.IO.FileSystem.DriveInfo", "b03f5f7f11d50a3a"),
        ("System.IO.FileSystem.Primitives", "b03f5f7f11d50a3a"),
        ("System.IO.FileSystem.Watcher", "b03f5f7f11d50a3a"),
        ("System.IO.IsolatedStorage", "b03f5f7f11d50a3a"),
        ("System.IO.MemoryMappedFiles", "b03f5f7f11d50a3a"),
        ("System.IO.Pipes.AccessControl", "b03f5f7f11d50a3a"),
        ("System.IO.Pipes", "b03f5f7f11d50a3a"),
        ("System.IO.UnmanagedMemoryStream", "b03f5f7f11d50a3a"),
        ("System.Linq", "b03f5f7f11d50a3a"),
        ("System.Linq.Expressions", "b03f5f7f11d50a3a"),
        ("System.Linq.Parallel", "b03f5f7f11d50a3a"),
        ("System.Linq.Queryable", "b03f5f7f11d50a3a"),
        ("System.Memory", "cc7b13ffcd2ddd51"),
        ("System.Net", "b03f5f7f11d50a3a"),
        ("System.Net.Http", "b03f5f7f11d50a3a"),
        ("System.Net.Http.Json", "cc7b13ffcd2ddd51"),
        ("System.Net.HttpListener", "cc7b13ffcd2ddd51"),
        ("System.Net.Mail", "cc7b13ffcd2ddd51"),
        ("System.Net.NameResolution", "b03f5f7f11d50a3a"),
        ("System.Net.NetworkInformation", "b03f5f7f11d50a3a"),
        ("System.Net.Ping", "b03f5f7f11d50a3a"),
        ("System.Net.Primitives", "b03f5f7f11d50a3a"),
        ("System.Net.Quic", "b03f5f7f11d50a3a"),
        ("System.Net.Requests", "b03f5f7f11d50a3a"),
        ("System.Net.Security", "b03f5f7f11d50a3a"),
        ("System.Net.ServicePoint", "cc7b13ffcd2ddd51"),
        ("System.Net.Sockets", "b03f5f7f11d50a3a"),
        ("System.Net.WebClient", "cc7b13ffcd2ddd51"),
        ("System.Net.WebHeaderCollection", "b03f5f7f11d50a3a"),
        ("System.Net.WebProxy", "cc7b13ffcd2ddd51"),
        ("System.Net.WebSockets.Client", "b03f5f7f11d50a3a"),
        ("System.Net.WebSockets", "b03f5f7f11d50a3a"),
        ("System.Numerics", "b77a5c561934e089"),
        ("System.Numerics.Vectors", "b03f5f7f11d50a3a"),
        ("System.ObjectModel", "b03f5f7f11d50a3a"),
        ("System.Private.CoreLib", "7cec85d7bea7798e"),
        ("System.Private.DataContractSerialization", "b03f5f7f11d50a3a"),
        ("System.Private.Uri", "b03f5f7f11d50a3a"),
        ("System.Private.Xml", "cc7b13ffcd2ddd51"),
        ("System.Private.Xml.Linq", "cc7b13ffcd2ddd51"),
        ("System.Reflection.DispatchProxy", "b03f5f7f11d50a3a"),
        ("System.Reflection", "b03f5f7f11d50a3a"),
        ("System.Reflection.Emit", "b03f5f7f11d50a3a"),
        ("System.Reflection.Emit.ILGeneration", "b03f5f7f11d50a3a"),
        ("System.Reflection.Emit.Lightweight", "b03f5f7f11d50a3a"),
        ("System.Reflection.Extensions", "b03f5f7f11d50a3a"),
        ("System.Reflection.Metadata", "b03f5f7f11d50a3a"),
        ("System.Reflection.Primitives", "b03f5f7f11d50a3a"),
        ("System.Reflection.TypeExtensions", "b03f5f7f11d50a3a"),
        ("System.Resources.Reader", "b03f5f7f11d50a3a"),
        ("System.Resources.ResourceManager", "b03f5f7f11d50a3a"),
        ("System.Resources.Writer", "b03f5f7f11d50a3a"),
        ("System.Runtime.CompilerServices.Unsafe", "b03f5f7f11d50a3a"),
        ("System.Runtime.CompilerServices.VisualC", "b03f5f7f11d50a3a"),
        ("System.Runtime", "b03f5f7f11d50a3a"),
        ("System.Runtime.Extensions", "b03f5f7f11d50a3a"),
        ("System.Runtime.Handles", "b03f5f7f11d50a3a"),
        ("System.Runtime.InteropServices", "b03f5f7f11d50a3a"),
        ("System.Runtime.InteropServices.JavaScript", "b03f5f7f11d50a3a"),
        ("System.Runtime.InteropServices.RuntimeInformation", "b03f5f7f11d50a3a"),
        ("System.Runtime.Intrinsics", "cc7b13ffcd2ddd51"),
        ("System.Runtime.Loader", "b03f5f7f11d50a3a"),
        ("System.Runtime.Numerics", "b03f5f7f11d50a3a"),
        ("System.Runtime.Serialization", "b77a5c561934e089"),
        ("System.Runtime.Serialization.Formatters", "b03f5f7f11d50a3a"),
        ("System.Runtime.Serialization.Json", "b03f5f7f11d50a3a"),
        ("System.Runtime.Serialization.Primitives", "b03f5f7f11d50a3a"),
        ("System.Runtime.Serialization.Xml", "b03f5f7f11d50a3a"),
        ("System.Security.AccessControl", "b03f5f7f11d50a3a"),
        ("System.Security.Claims", "b03f5f7f11d50a3a"),
        ("System.Security.Cryptography.Algorithms", "b03f5f7f11d50a3a"),
        ("System.Security.Cryptography.Cng", "b03f5f7f11d50a3a"),
        ("System.Security.Cryptography.Csp", "b03f5f7f11d50a3a"),
        ("System.Security.Cryptography", "b03f5f7f11d50a3a"),
        ("System.Security.Cryptography.Encoding", "b03f5f7f11d50a3a"),
        ("System.Security.Cryptography.OpenSsl", "b03f5f7f11d50a3a"),
        ("System.Security.Cryptography.Primitives", "b03f5f7f11d50a3a"),
        ("System.Security.Cryptography.X509Certificates", "b03f5f7f11d50a3a"),
        ("System.Security", "b03f5f7f11d50a3a"),
        ("System.Security.Principal", "b03f5f7f11d50a3a"),
        ("System.Security.Principal.Windows", "b03f5f7f11d50a3a"),
        ("System.Security.SecureString", "b03f5f7f11d50a3a"),
        ("System.ServiceModel.Web", "31bf3856ad364e35"),
        ("System.ServiceProcess", "b03f5f7f11d50a3a"),
        ("System.Text.Encoding.CodePages", "b03f5f7f11d50a3a"),
        ("System.Text.Encoding", "b03f5f7f11d50a3a"),
        ("System.Text.Encoding.Extensions", "b03f5f7f11d50a3a"),
        ("System.Text.Encodings.Web", "cc7b13ffcd2ddd51"),
        ("System.Text.Json", "cc7b13ffcd2ddd51"),
        ("System.Text.RegularExpressions", "b03f5f7f11d50a3a"),
        ("System.Threading.Channels", "cc7b13ffcd2ddd51"),
        ("System.Threading", "b03f5f7f11d50a3a"),
        ("System.Threading.Overlapped", "b03f5f7f11d50a3a"),
        ("System.Threading.Tasks.Dataflow", "b03f5f7f11d50a3a"),
        ("System.Threading.Tasks", "b03f5f7f11d50a3a"),
        ("System.Threading.Tasks.Extensions", "cc7b13ffcd2ddd51"),
        ("System.Threading.Tasks.Parallel", "b03f5f7f11d50a3a"),
        ("System.Threading.Thread", "b03f5f7f11d50a3a"),
        ("System.Threading.ThreadPool", "b03f5f7f11d50a3a"),
        ("System.Threading.Timer", "b03f5f7f11d50a3a"),
        ("System.Transactions", "b77a5c561934e089"),
        ("System.Transactions.Local", "cc7b13ffcd2ddd51"),
        ("System.ValueTuple", "cc7b13ffcd2ddd51"),
        ("System.Web", "b03f5f7f11d50a3a"),
        ("System.Web.HttpUtility", "cc7b13ffcd2ddd51"),
        ("System.Windows", "b03f5f7f11d50a3a"),
        ("System.Xml", "b77a5c561934e089"),
        ("System.Xml.Linq", "b77a5c561934e089"),
        ("System.Xml.ReaderWriter", "b03f5f7f11d50a3a"),
        ("System.Xml.Serialization", "b77a5c561934e089"),
        ("System.Xml.XDocument", "b03f5f7f11d50a3a"),
        ("System.Xml.XmlDocument", "b03f5f7f11d50a3a"),
        ("System.Xml.XmlSerializer", "b03f5f7f11d50a3a"),
        ("System.Xml.XPath", "b03f5f7f11d50a3a"),
        ("System.Xml.XPath.XDocument", "b03f5f7f11d50a3a"),
        ("WindowsBase", "31bf3856ad364e35"),
      };
    }
  }
}

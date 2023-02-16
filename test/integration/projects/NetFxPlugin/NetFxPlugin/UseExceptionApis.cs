using Microsoft.CSharp;
using Rhino.Display;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestPlugin
{
  public class UseExceptionApis
  {
    UseExceptionApis()
    {
      var str = "Hello World!";
      var mesh = new Point3d(0.0, 0.0, 0.0);
      Color4f.FromArgb(0.0f, 0.0f, 0.0f, 0.0f);

      var provider = new CSharpCodeProvider();

      // CodeDom - throws NotSupportedException
      provider.GenerateCodeFromMember(new System.CodeDom.CodeTypeMember(), new StringWriter(), new System.CodeDom.Compiler.CodeGeneratorOptions());

      // CodeDom - throws PlatformNotSupportedException
      var results = provider.CompileAssemblyFromSource(new System.CodeDom.Compiler.CompilerParameters(), "woot");
      var results2 = provider.CompileAssemblyFromFile(new System.CodeDom.Compiler.CompilerParameters(), "woot");
      var results3 = provider.CompileAssemblyFromDom(new System.CodeDom.Compiler.CompilerParameters(), new System.CodeDom.CodeCompileUnit());

      // AppDomain - throws PlatformNotSupportedException
      var domain = AppDomain.CreateDomain("MyDomain");
      AppDomain.CurrentDomain.ExecuteAssembly("myAssemblyFile");
      AppDomain.CurrentDomain.ExecuteAssembly("myAssemblyFile", new string[] { "woo" });
      AppDomain.CurrentDomain.ExecuteAssembly("myAssemblyFile", new string[] { "woo" }, null, System.Configuration.Assemblies.AssemblyHashAlgorithm.None);
    }
  }
}

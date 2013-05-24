using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;

using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;

namespace Metarx
{
    public static class Rose
    {
        public static Assembly Compile(string source) 
        {
            var tree = SyntaxTree.ParseText(source);
            var dllName = "metarx" + Guid.NewGuid().ToString().Replace("-", "") + ".dll";
            return Compile(tree, dllName);        
        }

        public static Assembly Compile(SyntaxTree tree, string dllName)
        {
            var myRefs =
                new[] { 
                    "System", "System.Core", "mscorlib", "System.Runtime"
                }.Select(MetadataReference.CreateAssemblyReference);

            var obsRef = new MetadataFileReference(typeof(Observable).Assembly.Location);

            myRefs = myRefs.Union(new [] {obsRef});

            var compiledCode = Compilation.Create(
                outputName: dllName,
                options: new CompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                syntaxTrees: new[] { tree },
                references: myRefs);

                
            using (var stream = new MemoryStream())
            {
                var emitResult = compiledCode.Emit(stream);
                if (!emitResult.Success)
                {
                    var message = string.Join("\r\n", emitResult.Diagnostics);
                    throw new ApplicationException(message);
                }

                return Assembly.Load(stream.GetBuffer());
            }
        }

        public static object GetSuitableType(Assembly asm) 
        {
            var type = asm.GetTypes().First(t => t.GetMethods().Any(m => m.Name == "Execute"));
            return Activator.CreateInstance(type);
        }

        public static string Execute(string program)
        {
            var asm = Compile(program);
            dynamic runnable = GetSuitableType(asm);
            var result = (string)runnable.Execute("quux!");
            return result;
        }

        public static dynamic CreateProgram(string program) {
            var asm = Compile(program);
            dynamic runnable = GetSuitableType(asm);
            return runnable;
        }
    }
}
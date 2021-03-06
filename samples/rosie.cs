using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;

using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;
using Roslyn.Compilers.Common;

namespace Metarx.Core
{
    public class Rosie
    {
        public IObservable<object> Execute(IObservable<Tuple<string, string>> stream)
        {
            return stream.Select(t => Rose.CreateProgram(t.Item2));
        }
    }

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
            var synRef = new MetadataFileReference(typeof(CommonSyntaxTree).Assembly.Location);
            var comRef = new MetadataFileReference(typeof(CompilationOptions).Assembly.Location);

            myRefs = myRefs.Union(new[] { obsRef, synRef, comRef });

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
            var type = asm.GetTypes().First(t => t.GetMethods().Any(m => m.Name == "Execute") && t.GetConstructors().Any(c => !c.GetParameters().Any()));
            return Activator.CreateInstance(type);
        }

        public static object CreateProgram(string program)
        {
            var asm = Compile(program);
            return GetSuitableType(asm);
        }
    }
}
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ProtoBuf;
using SharpLab.Container;
using SharpLab.Container.Protocol.Stdin;
using SharpLab.Runtime.Internal;
using SharpLab.Server.Common.Internal;

namespace SharpLab.Tests.Of.Container.Internal {
    public class ContainerTestDriver {
        public static string CompileAndExecute(string code) {
            var executeCommand = new ExecuteCommand(Compile(code), "OUTPUT-END");

            var stdin = new MemoryStream();
            Serializer.SerializeWithLengthPrefix(stdin, executeCommand, PrefixStyle.Base128);
            stdin.Seek(0, SeekOrigin.Begin);

            var stdout = new MemoryStream();

            // Requied to ensure RuntimeServices from different tests do not interfere with each other
            var containerContext = new AssemblyLoadContext(null, isCollectible: true);
            containerContext.LoadFromAssemblyPath(typeof(ProtoWriter).Assembly.Location);
            containerContext.LoadFromAssemblyPath(typeof(Serializer).Assembly.Location);
            containerContext.LoadFromAssemblyPath(typeof(RuntimeServices).Assembly.Location);
            containerContext.LoadFromAssemblyPath(typeof(Program).Assembly.Location)
                .GetType(typeof(Program).FullName!, throwOnError: true)!
                .GetMethod(nameof(Program.Run), BindingFlags.Static | BindingFlags.NonPublic)!
                .Invoke(null, new[] { stdin, stdout });
            containerContext.Unload();

            return Encoding.UTF8.GetString(stdout.ToArray());
        }

        private static byte[] Compile(string code) {
            var references = new AssemblyReferenceCollector().SlowGetAllReferencedAssembliesRecursive(
                typeof(object).Assembly,
                typeof(SharpLabObjectExtensions).Assembly
            ).Select(a => MetadataReference.CreateFromFile(a.Location));
            var compilation = CSharpCompilation.Create("_", new[] { CSharpSyntaxTree.ParseText(code) }, references);

            var assemblyStream = new MemoryStream();
            var emitResult = compilation.Emit(assemblyStream);
            if (!emitResult.Success)
                throw new Exception("Compilation failed:\n" + string.Join('\n', emitResult.Diagnostics));
            return assemblyStream.ToArray();
        }
    }
}

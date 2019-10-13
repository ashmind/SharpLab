using System.IO;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using AshMind.Extensions;

namespace SharpLab.Server.Common.Internal {
    public class LocalAssemblyDocumentationResolver : IAssemblyDocumentationResolver {
        public DocumentationProvider? GetDocumentation([NotNull] Assembly assembly) {
            var fileName = Path.ChangeExtension(assembly.GetAssemblyFileFromCodeBase().Name, ".xml")!;
            if (fileName == "System.Private.CoreLib.xml")
                fileName = "System.Runtime.xml";

            var fullPath = Path.Combine(Path.GetDirectoryName(Current.AssemblyPath)!, "xmldocs", fileName);

            if (!File.Exists(fullPath))
                return null;

            return XmlDocumentationProvider.CreateFromFile(fullPath);
        }
    }
}

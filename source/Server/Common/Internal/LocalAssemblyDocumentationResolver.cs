using System.IO;
using Microsoft.CodeAnalysis;

namespace SharpLab.Server.Common.Internal {
    public class LocalAssemblyDocumentationResolver : IAssemblyDocumentationResolver {
        public DocumentationProvider? GetDocumentation(string assemblyPath) {
            Argument.NotNullOrEmpty(nameof(assemblyPath), assemblyPath);

            var fileName = Path.GetFileNameWithoutExtension(assemblyPath) + ".xml";
            if (fileName == "System.Private.CoreLib.xml")
                fileName = "System.Runtime.xml";

            var fullPath = Path.Combine(Path.GetDirectoryName(Current.AssemblyPath)!, "xmldocs", fileName);
            if (!File.Exists(fullPath))
                return null;

            return XmlDocumentationProvider.CreateFromFile(fullPath);
        }
    }
}

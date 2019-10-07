using System.Reflection;
using Microsoft.CodeAnalysis;

namespace SharpLab.Server.Common.Internal {
    public interface IAssemblyDocumentationResolver {
        DocumentationProvider? GetDocumentation(Assembly assembly);
    }
}
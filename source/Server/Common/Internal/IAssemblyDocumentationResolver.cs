using System.Reflection;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;

namespace SharpLab.Server.Common.Internal {
    public interface IAssemblyDocumentationResolver {
        [CanBeNull] DocumentationProvider GetDocumentation([NotNull] Assembly assembly);
    }
}
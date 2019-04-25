using System.Collections.Generic;
using System.IO;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using AshMind.Extensions;
using System;
using SharpLab.Server.Common.Internal;

namespace SharpLab.Server.Owin.Platform {
    public class Net47AssemblyDocumentationResolver : IAssemblyDocumentationResolver {
        private static readonly string ReferenceAssemblyRootPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
            + @"\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7";

        public DocumentationProvider GetDocumentation([NotNull] Assembly assembly) {
            foreach (var xmlPath in GetCandidatePaths(assembly)) {
                if (File.Exists(xmlPath))
                    return XmlDocumentationProvider.CreateFromFile(xmlPath);
            }

            return null;
        }

        private IEnumerable<string> GetCandidatePaths(Assembly assembly) {
            var file = assembly.GetAssemblyFileFromCodeBase();

            yield return Path.ChangeExtension(file.FullName, ".xml");
            yield return Path.Combine(ReferenceAssemblyRootPath, Path.ChangeExtension(file.Name, ".xml"));
        }
    }
}

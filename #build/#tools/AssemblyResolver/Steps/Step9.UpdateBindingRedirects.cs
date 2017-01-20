using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using AssemblyResolver.Common;

namespace AssemblyResolver.Steps {
    public static class Step9 {
        public static void UpdateBindingRedirects(string applicationConfigurationPath, IEnumerable<AssemblyDetails> references) {
            FluentConsole.White.Line("Updating binding redirects…");
            var xml = XDocument.Load(applicationConfigurationPath);
            var asm = XNamespace.Get("urn:schemas-microsoft-com:asm.v1");
            var assemblyBinding = xml.Descendants("runtime").Elements(asm + "assemblyBinding").Single();
            foreach (var assembly in references) {
                FluentConsole.Gray.Line($"  {assembly.Definition.FullName}");
                var name = assembly.Definition.Name.Name;
                var assemblyIdentity = assemblyBinding.Descendants(asm + "assemblyIdentity").FirstOrDefault(x => x.Attribute("name")?.Value == name);
                var dependentAssembly = assemblyIdentity?.Parent;
                var bindingRedirect = dependentAssembly?.Element(asm + "bindingRedirect");
                if (assemblyIdentity == null) {
                    assemblyIdentity = new XElement(
                        asm + "assemblyIdentity",
                        new XAttribute("name", name),
                        new XAttribute("publicKeyToken", BitConverter.ToString(assembly.Definition.Name.PublicKeyToken).Replace("-", "").ToLowerInvariant())
                    );
                    bindingRedirect = new XElement(asm + "bindingRedirect");
                    dependentAssembly = new XElement(asm + "dependentAssembly", assemblyIdentity, bindingRedirect);
                    assemblyBinding.Add(dependentAssembly);
                }
                // ReSharper disable once PossibleNullReferenceException
                bindingRedirect.SetAttributeValue("oldVersion", "0.0.0.0-" + assembly.Definition.Name.Version);
                bindingRedirect.SetAttributeValue("newVersion", assembly.Definition.Name.Version);
            }
            xml.Save(applicationConfigurationPath);
        }
    }
}

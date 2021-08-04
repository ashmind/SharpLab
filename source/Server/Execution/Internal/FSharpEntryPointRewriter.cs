using Microsoft.FSharp.Core;
using MirrorSharp.Advanced;
using MirrorSharp.FSharp.Advanced;
using Mono.Cecil;

namespace SharpLab.Server.Execution.Internal {
    // There are some weird problems when I try to compile F# code as an exe (e.g. it tries to
    // do filesystem operations without using the virtual filesystem), so instead I compile
    // it as a library and then fake the entry point.
    public class FSharpEntryPointRewriter : IAssemblyRewriter, IContainerAssemblyRewriter {
        public void Rewrite(AssemblyDefinition assembly, IWorkSession session) {
            if (!session.IsFSharp())
                return;

            if (assembly.EntryPoint != null)
                return;
            
            var (entryPoint, isStaticConstructor) = FindBestEntryPointCandidate(assembly);
            if (entryPoint == null)
                return;

            if (isStaticConstructor) {
                entryPoint.Attributes &= ~MethodAttributes.SpecialName & ~MethodAttributes.RTSpecialName;
                entryPoint.Name = "cctor_rewritten_to_method_by_sharplab";
            }
            assembly.EntryPoint = entryPoint;
        }

        private (MethodDefinition? method, bool isStaticConstructor) FindBestEntryPointCandidate(AssemblyDefinition assembly) {
            // First priority -- explicit [<EntryPoint>]
            // Second priority -- top level code (gets compiled into a static ctor)

            MethodDefinition? startup = null;
            foreach (var type in assembly.MainModule.Types) {
                if (type.Namespace == "<StartupCode$_>" && type.Name == "$_" && type.HasMethods) {
                    foreach (var method in type.Methods) {
                        if (method.IsConstructor && method.IsStatic) {
                            startup = method;
                            break;
                        }
                    }
                    continue;
                }

                if (type.Namespace == "" && type.Name == "_" && type.HasMethods) {
                    foreach (var method in type.Methods) {
                        if (HasEntryPointAttribute(method))
                            return (method, false);
                    }
                }
            }

            return (startup, startup != null);
        }

        private bool HasEntryPointAttribute(MethodDefinition method) {
            if (!method.HasCustomAttributes)
                return false;

            foreach (var attribute in method.CustomAttributes) {
                if (attribute.AttributeType.Namespace == "Microsoft.FSharp.Core" && attribute.AttributeType.Name == nameof(EntryPointAttribute))
                    return true;
            }
            return false;
        }
    }
}
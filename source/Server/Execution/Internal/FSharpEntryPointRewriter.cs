using Microsoft.FSharp.Core;
using MirrorSharp.Advanced;
using MirrorSharp.FSharp.Advanced;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Runtime.CompilerServices;
using MethodInfo = System.Reflection.MethodInfo;

namespace SharpLab.Server.Execution.Internal {
    // There are some weird problems when I try to compile F# code as an exe (e.g. it tries to
    // do filesystem operations without using the virtual filesystem), so instead I compile
    // it as a library and then fake the entry point.
    public class FSharpEntryPointRewriter : IAssemblyRewriter {
        private static readonly MethodInfo RunClassConstructorMethod =
            typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.RunClassConstructor))!;

        public void Rewrite(ModuleDefinition module, IWorkSession session) {
            if (!session.IsFSharp())
                return;

            if (module.EntryPoint != null)
                return;
            
            var (entryPoint, isStaticConstructor) = FindBestEntryPointCandidate(module);
            if (entryPoint == null)
                return;

            if (isStaticConstructor)
                entryPoint = CreateEntryPointForStaticConstructor(entryPoint);
            module.EntryPoint = entryPoint;
        }

        private MethodDefinition CreateEntryPointForStaticConstructor(MethodDefinition constructor) {
            var module = constructor.Module;
            var entryPoint = new MethodDefinition(
                "entrypoint_generated_by_sharplab",
                MethodAttributes.Static,
                module.ImportReference(typeof(void))
            );
            entryPoint.Body = new MethodBody(entryPoint);

            var runClassConstructor = module.ImportReference(RunClassConstructorMethod);
            var entryPointIL = entryPoint.Body.GetILProcessor();
            entryPoint.Body.MaxStackSize = 1;
            entryPoint.Body.Instructions.Add(entryPointIL.Create(OpCodes.Ldtoken, constructor.DeclaringType));
            entryPoint.Body.Instructions.Add(entryPointIL.CreateCall(runClassConstructor));
            entryPoint.Body.Instructions.Add(entryPointIL.Create(OpCodes.Ret));

            constructor.DeclaringType.Methods.Add(entryPoint);
            return entryPoint;
        }

        private (MethodDefinition? method, bool isStaticConstructor) FindBestEntryPointCandidate(ModuleDefinition module) {
            if (!module.HasTypes)
                return (null, false);

            // First priority -- explicit [<EntryPoint>]
            // Second priority -- top level code (gets compiled into a static ctor)

            var startup = (MethodDefinition?)null;
            foreach (var type in module.Types) {
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
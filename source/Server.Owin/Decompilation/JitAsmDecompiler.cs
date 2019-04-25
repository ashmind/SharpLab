using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using AppDomainToolkit;
using AshMind.Extensions;
using JetBrains.Annotations;
using Microsoft.Diagnostics.Runtime;
using SharpLab.Server.Decompilation;
using SharpLab.Server.Decompilation.Internal;

namespace SharpLab.Server.Owin.Decompilation {
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    public class JitAsmDecompiler : JitAsmDecompilerBase {
        protected override JitAsmResultScope JitCompileAndGetMethods(MemoryStream assemblyStream) {
            AppDomainContext<AssemblyTargetLoader, PathBasedAssemblyResolver> context = null;
            try {
                var currentSetup = AppDomain.CurrentDomain.SetupInformation;
                context = AppDomainContext.Create(new AppDomainSetup {
                    ApplicationBase = currentSetup.ApplicationBase,
                    PrivateBinPath = currentSetup.PrivateBinPath
                });
                context.LoadAssembly(LoadMethod.LoadFrom, typeof(JitAsmDecompiler).Assembly.GetAssemblyFile().FullName);
                var results = RemoteFunc.Invoke(context.Domain, assemblyStream.ToArray(), Remote.GetCompiledMethods);

                return new JitAsmResultScope(results, context);
            }
            catch {
                context?.Dispose();
                throw;
            }
        }

        protected override ClrFlavor ClrFlavor => ClrFlavor.Desktop;

        private static class Remote {
            public static IReadOnlyList<MethodJitResult> GetCompiledMethods(byte[] assemblyBytes) {
                var assembly = Assembly.Load(assemblyBytes);
                return IsolatedJitAsmDecompilerCore.JitCompileAndGetMethods(assembly);
            }
        }
    }
}
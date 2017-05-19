using System.Collections.Immutable;
using AshMind.Extensions;
using JetBrains.Annotations;
using Microsoft.FSharp.Core;
using MirrorSharp;
using MirrorSharp.FSharp;
using SharpLab.Runtime;
using SharpLab.Server.MirrorSharp;

namespace SharpLab.Server.Compilation {
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    public class FSharpSetup : IMirrorSharpSetup {
        public void ApplyTo(MirrorSharpOptions options) {
            options.EnableFSharp(o => o.AssemblyReferencePaths = ImmutableArray.Create(
                typeof(object).Assembly.Location,
                // have to use codebase here to make sure .optdata and .sigdata are found
                typeof(FSharpOption<>).Assembly.GetAssemblyFileFromCodeBase().FullName,
                typeof(JitGenericAttribute).Assembly.Location
            ));
        }
    }
}
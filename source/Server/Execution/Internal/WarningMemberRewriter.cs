using System;
using System.Reflection;
using JetBrains.Annotations;
using Mono.Cecil.Cil;
using SharpLab.Runtime.Internal;
using Unbreakable.Policy.Internal;

namespace SharpLab.Server.Execution.Internal {
    public class WarningMemberRewriter : IMemberRewriterInternal {
        private static readonly MethodInfo WriteWarningMethod = ((Action<string>)(Output.WriteWarning)).Method;
        private readonly string _warning;

        public WarningMemberRewriter([NotNull] string warning) {
            _warning = Argument.NotNullOrEmpty(nameof(warning), warning);
        }

        string IMemberRewriterInternal.GetShortName() => $"{GetType().Name}(\"{_warning}\")";

        bool IMemberRewriterInternal.Rewrite(Instruction instruction, MemberRewriterContext context) {
            var writeWarning = context.IL.Body.Method.Module.ImportReference(WriteWarningMethod);

            var ldstr = context.IL.Create(OpCodes.Ldstr, _warning);
            context.IL.InsertAfter(instruction, ldstr);
            context.IL.InsertAfter(ldstr, context.IL.CreateCall(writeWarning));
            return true;
        }
    }
}

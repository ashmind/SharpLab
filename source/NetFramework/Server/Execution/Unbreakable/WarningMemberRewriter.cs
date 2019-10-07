using System;
using System.Reflection;
using JetBrains.Annotations;
using Mono.Cecil.Cil;
using SharpLab.Runtime.Internal;
using Unbreakable.Policy.Internal;
using SharpLab.Server.Execution.Internal;

namespace SharpLab.Server.Execution.Unbreakable {
    public class WarningMemberRewriter : IMemberRewriterInternal {
        private static readonly MethodInfo WriteWarningMethod = ((Action<string>)(Output.WriteWarning)).Method;
        private readonly string _warning;

        public WarningMemberRewriter([NotNull] string warning) {
            _warning = Argument.NotNullOrEmpty(nameof(warning), warning);
        }

        string IMemberRewriterInternal.GetShortName() => $"{nameof(WarningMemberRewriter)}(\"{_warning}\")";

        bool IMemberRewriterInternal.Rewrite(Instruction instruction, MemberRewriterContext context) {
            var writeWarning = context.IL.Body.Method.Module.ImportReference(WriteWarningMethod);

            var ldstr = context.IL.Create(OpCodes.Ldstr, _warning);
            context.IL.InsertAfter(instruction, ldstr);
            context.IL.InsertAfter(ldstr, context.IL.CreateCall(writeWarning));
            return true;
        }
    }
}

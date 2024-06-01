using Mono.Cecil;
using Mono.Cecil.Cil;
using Unbreakable.Policy.Internal;
using SharpLab.Server.Execution.Internal;

namespace SharpLab.Server.Execution.Unbreakable;

internal class ArrayReturnRewriter : IMemberRewriterInternal {
    public static ArrayReturnRewriter Default { get; } = new ArrayReturnRewriter();

    public string GetShortName() => nameof(ArrayReturnRewriter);

    public bool Rewrite(Instruction instruction, MemberRewriterContext context) {
        var il = context.IL;

        var method = ((MethodReference)instruction.Operand).Resolve();
        if (!method.ReturnType.IsArray)
            return false;

        var dup = il.Create(OpCodes.Dup);
        var ldlen = il.Create(OpCodes.Ldlen);
        var ldloc = il.CreateLdlocBest(context.RuntimeGuardVariable);
        var call = il.CreateCall(context.RuntimeGuardReferences.FlowThroughGuardCountIntPtrMethod);
        var pop = il.Create(OpCodes.Pop);

        context.IL.InsertAfter(instruction, dup);
        context.IL.InsertAfter(dup, ldlen);
        context.IL.InsertAfter(ldlen, ldloc);
        context.IL.InsertAfter(ldloc, call);
        context.IL.InsertAfter(call, pop);
        return true;
    }
}

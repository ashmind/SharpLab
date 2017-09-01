using JetBrains.Annotations;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace SharpLab.Server.Execution.Internal {
    internal static class CecilExtensions {
        public static Instruction CreateLdargBest(this ILProcessor il, ParameterReference parameter) {
            switch (parameter.Index) {
                case 0: return il.Create(OpCodes.Ldarg_0);
                case 1: return il.Create(OpCodes.Ldarg_1);
                case 2: return il.Create(OpCodes.Ldarg_2);
                case 3: return il.Create(OpCodes.Ldarg_3);
                default:
                    if (IsSByte(parameter.Index))
                        return il.Create(OpCodes.Ldarg_S, (sbyte)parameter.Index);
                    return il.Create(OpCodes.Ldarg, parameter.Index);
            }
        }

        public static Instruction CreateLdlocBest(this ILProcessor il, VariableDefinition variable) {
            switch (variable.Index) {
                case 0:  return il.Create(OpCodes.Ldloc_0);
                case 1:  return il.Create(OpCodes.Ldloc_1);
                case 2:  return il.Create(OpCodes.Ldloc_2);
                case 3:  return il.Create(OpCodes.Ldloc_3);
                default:
                    if (IsSByte(variable.Index))
                        return il.Create(OpCodes.Ldloc_S, (sbyte)variable.Index);
                    return il.Create(OpCodes.Ldloc, variable);
            }
        }

        public static Instruction CreateStlocBest(this ILProcessor il, VariableDefinition variable) {
            switch (variable.Index) {
                case 0:  return il.Create(OpCodes.Stloc_0);
                case 1:  return il.Create(OpCodes.Stloc_1);
                case 2:  return il.Create(OpCodes.Stloc_2);
                case 3:  return il.Create(OpCodes.Stloc_3);
                default:
                    if (IsSByte(variable.Index))
                        return il.Create(OpCodes.Stloc_S, (sbyte)variable.Index);
                    return il.Create(OpCodes.Stloc, variable);
            }
        }

        public static Instruction CreateLdcI4Best(this ILProcessor il, int value) {
            switch (value) {
                case 0: return il.Create(OpCodes.Ldc_I4_0);
                case 1: return il.Create(OpCodes.Ldc_I4_1);
                case 2: return il.Create(OpCodes.Ldc_I4_2);
                case 3: return il.Create(OpCodes.Ldc_I4_3);
                case 4: return il.Create(OpCodes.Ldc_I4_4);
                case 5: return il.Create(OpCodes.Ldc_I4_5);
                case 6: return il.Create(OpCodes.Ldc_I4_6);
                case 7: return il.Create(OpCodes.Ldc_I4_7);
                case 8: return il.Create(OpCodes.Ldc_I4_8);
                case -1: return il.Create(OpCodes.Ldc_I4_M1);
                default:
                    if (IsSByte(value))
                        return il.Create(OpCodes.Ldc_I4_S, (sbyte)value);
                    return il.Create(OpCodes.Ldc_I4, value);
            }
        }

        private static bool IsSByte(int value) {
            return value >= sbyte.MinValue && value <= sbyte.MaxValue;
        }

        public static Instruction CreateCall(this ILProcessor il, MethodReference method) {
            return il.Create(OpCodes.Call, method);
        }

        public static void RetargetAll(this ExceptionHandler handler, Instruction from, Instruction to) {
            if (handler.TryStart == from)
                handler.TryStart = to;
            if (handler.TryEnd == from)
                handler.TryEnd = to;
            if (handler.FilterStart == from)
                handler.FilterStart = to;
            if (handler.HandlerStart == from)
                handler.HandlerStart = to;
            if (handler.HandlerEnd == from)
                handler.HandlerEnd = to;
        }

        public static bool IsVoid([NotNull] this TypeReference type) {
            return type.Name == "Void" && type.Namespace == "System";
        }
    }
}

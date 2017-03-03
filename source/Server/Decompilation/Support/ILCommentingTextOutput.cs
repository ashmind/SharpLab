using System;
using System.Collections.Generic;
using AshMind.Extensions;
using ICSharpCode.Decompiler;
using ICSharpCode.NRefactory;
using Mono.Cecil.Cil;

namespace TryRoslyn.Server.Decompilation.Support {
    public class ILCommentingTextOutput : ITextOutput {
        private readonly ITextOutput _inner;
        private readonly int _commentMinColumn;

        private int _currentLineLength;
        private string _curentComment;

        public ILCommentingTextOutput(ITextOutput inner, int commentMinColumn) {
            if (inner == null)
                throw new ArgumentNullException(nameof(inner));

            _inner = inner;
            _commentMinColumn = commentMinColumn;
        }

        public TextLocation Location => _inner.Location;

        public void Indent() {
            _inner.Indent();
        }

        public void Unindent() {
            _inner.Unindent();
        }

        public void Write(char ch) {
            _inner.Write(ch);
            _currentLineLength += 1;
        }

        public void Write(string text) {
            _inner.Write(text);
            _currentLineLength += text.Length;
        }

        public void WriteLine() {
            WriteComment();
            _inner.WriteLine();
            _currentLineLength = 0;
        }

        public void WriteDefinition(string text, object definition, bool isLocal = true) {
            _inner.WriteDefinition(text, definition, isLocal);
            _currentLineLength += text.Length;
        }

        public void WriteReference(string text, object reference, bool isLocal = false) {
            UpdateCurrentCommentFrom(reference);
            _inner.WriteReference(text, reference, isLocal);
            _currentLineLength += text.Length;
        }

        void ITextOutput.MarkFoldStart(string collapsedText, bool defaultCollapsed) => _inner.MarkFoldStart(collapsedText, defaultCollapsed);
        void ITextOutput.MarkFoldEnd() => _inner.MarkFoldEnd();
        void ITextOutput.AddDebuggerMemberMapping(MemberMapping memberMapping) => _inner.AddDebuggerMemberMapping(memberMapping);

        private void UpdateCurrentCommentFrom(object reference) {
            if (!(reference is OpCode))
                return;

            var comment = Comments.GetValueOrDefault((OpCode)reference);
            if (comment != null)
                _curentComment = comment;
        }

        private void WriteComment() {
            if (_curentComment == null)
                return;

            _inner.Write(new string(' ', Math.Max(_commentMinColumn - _currentLineLength, 1)));
            _inner.Write($"// {_curentComment}");
            _curentComment = null;
        }

        private static readonly IReadOnlyDictionary<OpCode, string> Comments = new Dictionary<OpCode, string> {
            [OpCodes.Add] = "Add two values, returning a new value",
            [OpCodes.Add_Ovf] = "Add signed integer values with overflow check",
            [OpCodes.Add_Ovf_Un] = "Add unsigned integer values with overflow check",
            [OpCodes.And] = "Bitwise AND of two integral values, returns an integral value",
            [OpCodes.Arglist] = "Return argument list handle for the current method",
            [OpCodes.Beq] = "Branch to target if equal",
            [OpCodes.Beq_S] = "Branch to target if equal, short form",
            [OpCodes.Bge] = "Branch to target if greater than or equal to",
            [OpCodes.Bge_S] = "Branch to target if greater than or equal to, short form",
            [OpCodes.Bge_Un] = "Branch to target if greater than or equal to (unsigned or unordered)",
            [OpCodes.Bge_Un_S] = "Branch to target if greater than or equal to (unsigned or unordered), short form",
            [OpCodes.Bgt] = "Branch to target if greater than",
            [OpCodes.Bgt_S] = "Branch to target if greater than, short form",
            [OpCodes.Bgt_Un] = "Branch to target if greater than (unsigned or unordered)",
            [OpCodes.Bgt_Un_S] = "Branch to target if greater than (unsigned or unordered), short form",
            [OpCodes.Ble] = "Branch to target if less than or equal to",
            [OpCodes.Ble_S] = "Branch to target if less than or equal to, short form",
            [OpCodes.Ble_Un] = "Branch to target if less than or equal to (unsigned or unordered)",
            [OpCodes.Ble_Un_S] = "Branch to target if less than or equal to (unsigned or unordered), short form",
            [OpCodes.Blt] = "Branch to target if less than",
            [OpCodes.Blt_S] = "Branch to target if less than, short form",
            [OpCodes.Blt_Un] = "Branch to target if less than (unsigned or unordered)",
            [OpCodes.Blt_Un_S] = "Branch to target if less than (unsigned or unordered), short form",
            [OpCodes.Bne_Un] = "Branch to target if unequal or unordered",
            [OpCodes.Bne_Un_S] = "Branch to target if unequal or unordered, short form",
            [OpCodes.Box] = "Convert a boxable value to its boxed form",
            [OpCodes.Br] = "Branch to target",
            [OpCodes.Br_S] = "Branch to target, short form",
            [OpCodes.Break] = "Inform a debugger that a breakpoint has been reached",
            [OpCodes.Brfalse] = "Branch to target if value is zero (false)",
            [OpCodes.Brfalse_S] = "Branch to target if value is zero (false), short form",
            [OpCodes.Brtrue] = "Branch to target if value is non-zero (true)",
            [OpCodes.Brtrue_S] = "Branch to target if value is non-zero (true), short form",
            [OpCodes.Call] = "Call method",
            [OpCodes.Call] = "Call method indicated on the stack with arguments",
            [OpCodes.Callvirt] = "Call a method associated with an object",
            [OpCodes.Castclass] = "Cast obj to class",
            [OpCodes.Ceq] = "Push 1 (of type int32) if value1 equals value2, else push 0",
            [OpCodes.Cgt] = "Push 1 (of type int32) if value1 > value2, else push 0",
            [OpCodes.Cgt_Un] = "Push 1 (of type int32) if value1 > value2, unsigned or unordered, else push 0",
            [OpCodes.Ckfinite] = "Throw ArithmeticException if value is not a finite number",
            [OpCodes.Clt] = "Push 1 (of type int32) if value1 < value2, else push 0",
            [OpCodes.Clt_Un] = "Push 1 (of type int32) if value1 < value2, unsigned or unordered, else push 0",
            [OpCodes.Constrained] = "Call a virtual method on a type constrained to be type T",
            [OpCodes.Conv_I] = "Convert to native int, pushing native int on stack",
            [OpCodes.Conv_I1] = "Convert to int8, pushing int32 on stack",
            [OpCodes.Conv_I2] = "Convert to int16, pushing int32 on stack",
            [OpCodes.Conv_I4] = "Convert to int32, pushing int32 on stack",
            [OpCodes.Conv_I8] = "Convert to int64, pushing int64 on stack",
            [OpCodes.Conv_Ovf_I] = "Convert to a native int (on the stack as native int) and throw an exception on overflow",
            [OpCodes.Conv_Ovf_I_Un] = "Convert unsigned to a native int (on the stack as native int) and throw an exception on overflow",
            [OpCodes.Conv_Ovf_I1] = "Convert to an int8 (on the stack as int32) and throw an exception on overflow",
            [OpCodes.Conv_Ovf_I1_Un] = "Convert unsigned to an int8 (on the stack as int32) and throw an exception on overflow",
            [OpCodes.Conv_Ovf_I2] = "Convert to an int16 (on the stack as int32) and throw an exception on overflow",
            [OpCodes.Conv_Ovf_I2_Un] = "Convert unsigned to an int16 (on the stack as int32) and throw an exception on overflow",
            [OpCodes.Conv_Ovf_I4] = "Convert to an int32 (on the stack as int32) and throw an exception on overflow",
            [OpCodes.Conv_Ovf_I4_Un] = "Convert unsigned to an int32 (on the stack as int32) and throw an exception on overflow",
            [OpCodes.Conv_Ovf_I8] = "Convert to an int64 (on the stack as int64) and throw an exception on overflow",
            [OpCodes.Conv_Ovf_I8_Un] = "Convert unsigned to an int64 (on the stack as int64) and throw an exception on overflow",
            [OpCodes.Conv_Ovf_U] = "Convert to a native unsigned int (on the stack as native int) and throw an exception on overflow",
            [OpCodes.Conv_Ovf_U_Un] = "Convert unsigned to a native unsigned int (on the stack as native int) and throw an exception on overflow",
            [OpCodes.Conv_Ovf_U1] = "Convert to an unsigned int8 (on the stack as int32) and throw an exception on overflow",
            [OpCodes.Conv_Ovf_U1_Un] = "Convert unsigned to an unsigned int8 (on the stack as int32) and throw an exception on overflow",
            [OpCodes.Conv_Ovf_U2] = "Convert to an unsigned int16 (on the stack as int32) and throw an exception on overflow",
            [OpCodes.Conv_Ovf_U2_Un] = "Convert unsigned to an unsigned int16 (on the stack as int32) and throw an exception on overflow",
            [OpCodes.Conv_Ovf_U4] = "Convert to an unsigned int32 (on the stack as int32) and throw an exception on overflow",
            [OpCodes.Conv_Ovf_U4_Un] = "Convert unsigned to an unsigned int32 (on the stack as int32) and throw an exception on overflow",
            [OpCodes.Conv_Ovf_U8] = "Convert to an unsigned int64 (on the stack as int64) and throw an exception on overflow",
            [OpCodes.Conv_Ovf_U8_Un] = "Convert unsigned to an unsigned int64 (on the stack as int64) and throw an exception on overflow",
            [OpCodes.Conv_R_Un] = "Convert unsigned integer to floating-point, pushing F on stack",
            [OpCodes.Conv_R4] = "Convert to float32, pushing F on stack",
            [OpCodes.Conv_R8] = "Convert to float64, pushing F on stack",
            [OpCodes.Conv_U] = "Convert to native unsigned int, pushing native int on stack",
            [OpCodes.Conv_U1] = "Convert to unsigned int8, pushing int32 on stack",
            [OpCodes.Conv_U2] = "Convert to unsigned int16, pushing int32 on stack",
            [OpCodes.Conv_U4] = "Convert to unsigned int32, pushing int32 on stack",
            [OpCodes.Conv_U8] = "Convert to unsigned int64, pushing int64 on stack",
            [OpCodes.Cpblk] = "Copy data from memory to memory",
            [OpCodes.Cpobj] = "Copy a value type from src to dest",
            [OpCodes.Div] = "Divide two values to return a quotient or floating-point result",
            [OpCodes.Div_Un] = "Divide two values, unsigned, returning a quotient",
            [OpCodes.Dup] = "Duplicate the value on the top of the stack",
            [OpCodes.Endfilter] = "End an exception handling filter clause",
            [OpCodes.Endfinally] = "End finally clause of an exception block",
            [OpCodes.Initblk] = "Set all bytes in a block of memory to a given byte value",
            [OpCodes.Initobj] = "Initialize the value at address dest",
            [OpCodes.Isinst] = "Test if obj is an instance of class, returning null or an instance of that class or interface",
            [OpCodes.Jmp] = "Exit current method and jump to the specified method",
            [OpCodes.Ldarg] = "Load argument numbered num onto the stack",
            [OpCodes.Ldarg_0] = "Load argument 0 onto the stack",
            [OpCodes.Ldarg_1] = "Load argument 1 onto the stack",
            [OpCodes.Ldarg_2] = "Load argument 2 onto the stack",
            [OpCodes.Ldarg_3] = "Load argument 3 onto the stack",
            [OpCodes.Ldarg_S] = "Load argument numbered num onto the stack, short form",
            [OpCodes.Ldarga] = "Fetch the address of argument argNum",
            [OpCodes.Ldarga_S] = "Fetch the address of argument argNum, short form",
            [OpCodes.Ldc_I4] = "Push num of type int32 onto the stack as int32",
            [OpCodes.Ldc_I4_0] = "Push 0 onto the stack as int32",
            [OpCodes.Ldc_I4_1] = "Push 1 onto the stack as int32",
            [OpCodes.Ldc_I4_2] = "Push 2 onto the stack as int32",
            [OpCodes.Ldc_I4_3] = "Push 3 onto the stack as int32",
            [OpCodes.Ldc_I4_4] = "Push 4 onto the stack as int32",
            [OpCodes.Ldc_I4_5] = "Push 5 onto the stack as int32",
            [OpCodes.Ldc_I4_6] = "Push 6 onto the stack as int32",
            [OpCodes.Ldc_I4_7] = "Push 7 onto the stack as int32",
            [OpCodes.Ldc_I4_8] = "Push 8 onto the stack as int32",
            [OpCodes.Ldc_I4_M1] = "Push -1 onto the stack as int32",
            [OpCodes.Ldc_I4_S] = "Push num onto the stack as int32, short form",
            [OpCodes.Ldc_I8] = "Push num of type int64 onto the stack as int64",
            [OpCodes.Ldc_R4 ] = "Push num of type float32 onto the stack as F",
            [OpCodes.Ldc_R8] = "Push num of type float64 onto the stack as F",
            [OpCodes.Ldelem_Any] = "Load the element at index onto the top of the stack",
            [OpCodes.Ldelem_I] = "Load the element with type native int at index onto the top of the stack as a native int",
            [OpCodes.Ldelem_I1] = "Load the element with type int8 at index onto the top of the stack as an int32",
            [OpCodes.Ldelem_I2] = "Load the element with type int16 at index onto the top of the stack as an int32",
            [OpCodes.Ldelem_I4] = "Load the element with type int32 at index onto the top of the stack as an int32",
            [OpCodes.Ldelem_I8] = "Load the element with type int64 at index onto the top of the stack as an int64",
            [OpCodes.Ldelem_R4] = "Load the element with type float32 at index onto the top of the stack as an F",
            [OpCodes.Ldelem_R8] = "Load the element with type float64 at index onto the top of the stack as an F",
            [OpCodes.Ldelem_Ref] = "Load the element at index onto the top of the stack as an O. The type of the O is the same as the element type of the array pushed on the CIL stack",
            [OpCodes.Ldelem_U1] = "Load the element with type unsigned int8 at index onto the top of the stack as an int32",
            [OpCodes.Ldelem_U2] = "Load the element with type unsigned int16 at index onto the top of the stack as an int32",
            [OpCodes.Ldelem_U4] = "Load the element with type unsigned int32 at index onto the top of the stack as an int32",
            [OpCodes.Ldelema] = "Load the address of element at index onto the top of the stack",
            [OpCodes.Ldfld] = "Push the value of field of object (or value type) obj, onto the stack",
            [OpCodes.Ldflda] = "Push the address of field of object obj on the stack",
            [OpCodes.Ldftn] = "Push a pointer to a method referenced by method, on the stack",
            [OpCodes.Ldind_I] = "Indirect load value of type native int as native int on the stack",
            [OpCodes.Ldind_I1] = "Indirect load value of type int8 as int32 on the stack",
            [OpCodes.Ldind_I2] = "Indirect load value of type int16 as int32 on the stack",
            [OpCodes.Ldind_I4] = "Indirect load value of type int32 as int32 on the stack",
            [OpCodes.Ldind_I8] = "Indirect load value of type int64 as int64 on the stack",
            [OpCodes.Ldind_R4] = "Indirect load value of type float32 as F on the stack",
            [OpCodes.Ldind_R8] = "Indirect load value of type float64 as F on the stack",
            [OpCodes.Ldind_Ref] = "Indirect load value of type object ref as O on the stack",
            [OpCodes.Ldind_U1] = "Indirect load value of type unsigned int8 as int32 on the stack",
            [OpCodes.Ldind_U2] = "Indirect load value of type unsigned int16 as int32 on the stack",
            [OpCodes.Ldind_U4] = "Indirect load value of type unsigned int32 as int32 on the stack",
            [OpCodes.Ldlen] = "Push the length (of type native unsigned int) of array on the stack",
            [OpCodes.Ldloc] = "Load local variable of index indx onto stack",
            [OpCodes.Ldloc_0] = "Load local variable 0 onto stack",
            [OpCodes.Ldloc_1] = "Load local variable 1 onto stack",
            [OpCodes.Ldloc_2] = "Load local variable 2 onto stack",
            [OpCodes.Ldloc_3] = "Load local variable 3 onto stack",
            [OpCodes.Ldloc_S] = "Load local variable of index indx onto stack, short form",
            [OpCodes.Ldloca] = "Load address of local variable with index indx",
            [OpCodes.Ldloca_S] = "Load address of local variable with index indx, short form",
            [OpCodes.Ldnull] = "Push a null reference on the stack",
            [OpCodes.Ldobj] = "Copy the value stored at address src to the stack",
            [OpCodes.Ldsfld] = "Push the value of field on the stack",
            [OpCodes.Ldsflda] = "Push the address of the static field, field, on the stack",
            [OpCodes.Ldstr] = "Push a string object for the literal string",
            [OpCodes.Ldtoken] = "Convert metadata token to its runtime representation",
            [OpCodes.Ldvirtftn] = "Push address of virtual method on the stack",
            [OpCodes.Leave] = "Exit a protected region of code",
            [OpCodes.Leave_S] = "Exit a protected region of code, short form",
            [OpCodes.Localloc] = "Allocate space from the local memory pool",
            [OpCodes.Mkrefany] = "Push a typed reference to ptr of type class onto the stack",
            [OpCodes.Mul] = "Multiply values",
            [OpCodes.Mul_Ovf] = "Multiply signed integer values. Signed result shall fit in same size",
            [OpCodes.Mul_Ovf_Un] = "Multiply unsigned integer values. Unsigned result shall fit in same size",
            [OpCodes.Neg] = "Negate value",
            [OpCodes.Newarr] = "Create a new array with elements of type etype",
            [OpCodes.Newobj] = "Allocate an uninitialized object or value type and call ctor",
            [OpCodes.No] = "The specified fault check(s) normally performed as part of the execution of the subsequent instruction can/shall be skipped",
            [OpCodes.Nop] = "Do nothing (No operation)",
            [OpCodes.Not] = "Bitwise complement (logical not)",
            [OpCodes.Or] = "Bitwise OR of two integer values, returns an integer",
            [OpCodes.Pop] = "Pop value from the stack",
            [OpCodes.Readonly] = "Specify that the subsequent array address operation performs no type check at runtime, and that it returns a controlled-mutability managed pointer",
            [OpCodes.Refanytype] = "Push the type token stored in a typed reference",
            [OpCodes.Refanyval] = "Push the address stored in a typed reference",
            [OpCodes.Rem] = "Remainder when dividing one value by another",
            [OpCodes.Rem_Un] = "Remainder when dividing one unsigned value by another",
            [OpCodes.Ret] = "Return from method, possibly with a value",
            [OpCodes.Rethrow] = "Rethrow the current exception",
            [OpCodes.Shl] = "Shift an integer left (shifting in zeros), return an integer",
            [OpCodes.Shr] = "Shift an integer right (shift in sign), return an integer",
            [OpCodes.Shr_Un] = "Shift an integer right (shift in zero), return an integer",
            [OpCodes.Sizeof] = "Push the size, in bytes, of a type as an unsigned int32",
            [OpCodes.Starg] = "Store value to the argument numbered num",
            [OpCodes.Starg_S] = "Store value to the argument numbered num, short form",
            [OpCodes.Stelem_Any] = "Replace array element at index with the value on the stack",
            [OpCodes.Stelem_I] = "Replace array element at index with the i value on the stack",
            [OpCodes.Stelem_I1] = "Replace array element at index with the int8 value on the stack",
            [OpCodes.Stelem_I2] = "Replace array element at index with the int16 value on the stack",
            [OpCodes.Stelem_I4] = "Replace array element at index with the int32 value on the stack",
            [OpCodes.Stelem_I8] = "Replace array element at index with the int64 value on the stack",
            [OpCodes.Stelem_R4] = "Replace array element at index with the float32 value on the stack",
            [OpCodes.Stelem_R8] = "Replace array element at index with the float64 value on the stack",
            [OpCodes.Stelem_Ref] = "Replace array element at index with the ref value on the stack",
            [OpCodes.Stfld] = "Replace the value of field of the object obj with value",
            [OpCodes.Stind_I] = "Store value of type native int into memory at address",
            [OpCodes.Stind_I1] = "Store value of type int8 into memory at address",
            [OpCodes.Stind_I2] = "Store value of type int16 into memory at address",
            [OpCodes.Stind_I4] = "Store value of type int32 into memory at address",
            [OpCodes.Stind_I8] = "Store value of type int64 into memory at address",
            [OpCodes.Stind_R4] = "Store value of type float32 into memory at address",
            [OpCodes.Stind_R8] = "Store value of type float64 into memory at address",
            [OpCodes.Stind_Ref] = "Store value of type object ref (type O) into memory at address",
            [OpCodes.Stloc] = "Pop a value from stack into local variable indx",
            [OpCodes.Stloc_0] = "Pop a value from stack into local variable 0",
            [OpCodes.Stloc_1] = "Pop a value from stack into local variable 1",
            [OpCodes.Stloc_2] = "Pop a value from stack into local variable 2",
            [OpCodes.Stloc_3] = "Pop a value from stack into local variable 3",
            [OpCodes.Stloc_S] = "Pop a value from stack into local variable indx, short form",
            [OpCodes.Stobj] = "Store a value of type typeTok at an address",
            [OpCodes.Stsfld] = "Replace the value of field with val",
            [OpCodes.Sub] = "Subtract value2 from value1, returning a new value",
            [OpCodes.Sub_Ovf] = "Subtract native int from a native int. Signed result shall fit in same size",
            [OpCodes.Sub_Ovf_Un] = "Subtract native unsigned int from a native unsigned int. Unsigned result shall fit in same size",
            [OpCodes.Switch] = "Jump to one of n values",
            [OpCodes.Tail] = "Subsequent call terminates current method",
            [OpCodes.Throw] = "Throw an exception",
            [OpCodes.Unaligned] = "Subsequent pointer instruction might be unaligned",
            [OpCodes.Unbox] = "Extract a value-type from obj, its boxed representation",
            [OpCodes.Unbox_Any] = "Extract a value-type from obj, its boxed representation",
            [OpCodes.Volatile] = "Subsequent pointer reference is volatile",
            [OpCodes.Xor] = "Bitwise XOR of integer values, returns an integer"
        };
    }
}

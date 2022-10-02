using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using AshMind.Extensions;
using SharpLab.Runtime;

namespace SharpLab.Server.Decompilation.Internal {
    public static class IsolatedJitAsmDecompilerCore {
        public static IReadOnlyList<MethodJitResult> JitCompileAndGetMethods(Assembly assembly) {
            EnsureNoJitSideEffects(assembly);
            var results = new List<MethodJitResult>();
            foreach (var type in assembly.DefinedTypes) {
                if (type.IsNested)
                    continue; // it's easier to handle nested generic types recursively, so we suppress all nested for consistency
                CompileAndCollectMembers(results, type);
            }
            return results;
        }

        // This is a security consideration as PrepareMethod calls static ctors and module initializers
        private static void EnsureNoJitSideEffects(Assembly assembly) {
            try {
                foreach (var type in assembly.DefinedTypes) {
                    foreach (var constructor in type.DeclaredConstructors) {
                        if (constructor.IsStatic)
                            throw new NotSupportedException($"Type {type} has a static constructor, which is not supported by SharpLab JIT decompiler.");
                    }

                    foreach (var method in type.DeclaredMethods) {
                        foreach (var attribute in method.CustomAttributes) {
                            if (attribute.AttributeType is { Name: "ModuleInitializerAttribute", Namespace: "System.Runtime.CompilerServices" })
                                throw new NotSupportedException($"Method {method} is a module initializer, which is not supported by SharpLab JIT decompiler.");
                        }
                    }
                }
            }
            catch (ReflectionTypeLoadException ex) {
                throw new NotSupportedException("Unable to validate whether code has static constructors or module initializers (not supported by SharpLab JIT decompiler).", ex);
            }
        }

        private static void CompileAndCollectMembers(ICollection<MethodJitResult> results, TypeInfo type, ImmutableArray<Type>? genericArgumentTypes = null) {
            if (type.IsGenericTypeDefinition) {
                if (TryCompileAndCollectMembersOfGeneric(results, type, genericArgumentTypes))
                    return;
            }

            foreach (var constructor in type.DeclaredConstructors) {
                CollectCompiledWraps(results, constructor);
            }

            foreach (var method in type.DeclaredMethods) {
                if (method.IsAbstract)
                    continue;
                CollectCompiledWraps(results, method);
            }

            foreach (var nested in type.DeclaredNestedTypes) {
                CompileAndCollectMembers(results, nested, genericArgumentTypes);
            }
        }

        private static bool TryCompileAndCollectMembersOfGeneric(ICollection<MethodJitResult> results, TypeInfo type, ImmutableArray<Type>? parentArgumentTypes = null) {
            var hadAttribute = false;
            foreach (var attribute in type.GetCustomAttributes<JitGenericAttribute>(false)) {
                hadAttribute = true;

                var fullArgumentTypes = (parentArgumentTypes ?? ImmutableArray<Type>.Empty)
                    .AddRange(attribute.ArgumentTypes);
                var genericInstance = type.MakeGenericType(fullArgumentTypes.ToArray());
                CompileAndCollectMembers(results, genericInstance.GetTypeInfo(), fullArgumentTypes);
            }
            if (hadAttribute)
                return true;

            if (parentArgumentTypes != null) {
                var genericInstance = type.MakeGenericType(parentArgumentTypes.Value.ToArray());
                CompileAndCollectMembers(results, genericInstance.GetTypeInfo(), parentArgumentTypes);
                return true;
            }

            return false;
        }

        private static void CollectCompiledWraps(ICollection<MethodJitResult> results, MethodBase method) {
            if ((method.MethodImplementationFlags & MethodImplAttributes.Runtime) == MethodImplAttributes.Runtime) {
                results.Add(new MethodJitResult(method.MethodHandle, MethodJitStatus.IgnoredRuntime));
                return;
            }

            if ((method.Attributes & MethodAttributes.PinvokeImpl) == MethodAttributes.PinvokeImpl) {
                results.Add(new MethodJitResult(method.MethodHandle, MethodJitStatus.IgnoredPInvoke));
                return;
            }

            if (method.DeclaringType?.IsGenericTypeDefinition ?? false) {
                results.Add(new MethodJitResult(method.MethodHandle, MethodJitStatus.IgnoredOpenGenericWithNoAttribute));
                return;
            }

            if (method.IsGenericMethodDefinition) {
                CollectCompiledGeneric(results, (MethodInfo)method);
                return;
            }

            results.Add(CompileAndWrapSimple(method));
        }

        private static void CollectCompiledGeneric(ICollection<MethodJitResult> results, MethodInfo method) {
            var hasAttribute = false;
            foreach (var attribute in method.GetCustomAttributes<JitGenericAttribute>()) {
                hasAttribute = true;
                var genericInstance = method.MakeGenericMethod(attribute.ArgumentTypes);
                results.Add(CompileAndWrapSimple(genericInstance));
            }
            if (!hasAttribute)
                results.Add(new MethodJitResult(method.MethodHandle, MethodJitStatus.IgnoredOpenGenericWithNoAttribute));
        }

        private static MethodJitResult CompileAndWrapSimple(MethodBase method) {
            var handle = method.MethodHandle;
            RuntimeHelpers.PrepareMethod(handle);
            var isGeneric = method.IsGenericMethod || (method.DeclaringType?.IsGenericType ?? false);
            return new MethodJitResult(method.MethodHandle, isGeneric ? MethodJitStatus.SuccessGeneric : MethodJitStatus.Success);
        }
    }
}
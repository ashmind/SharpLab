using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using AshMind.Extensions;
using Microsoft.CodeAnalysis;
using MirrorSharp.Advanced;

namespace SharpLab.Server.Decompilation.Internal {
    using SerializePropertiesAction = Action<IOperation, IFastJsonWriter>;

    public class RoslynOperationPropertySerializer : IRoslynOperationPropertySerializer {
        private static readonly MethodInfo ObjectToString = typeof(object).GetMethod(nameof(ToString))!;
        private static readonly HashSet<Type> DirectlyWritableTypes = new HashSet<Type>(
            typeof(IFastJsonWriter)
                .GetMethods()
                .Where(m => m.Name == nameof(IFastJsonWriter.WriteValue))
                .Select(m => m.GetParameters()[0].ParameterType)
        );

        private readonly ConcurrentDictionary<Type, SerializePropertiesAction> _cache
            = new ConcurrentDictionary<Type, SerializePropertiesAction>();

        public void SerializeProperties(IOperation operation, IFastJsonWriter writer) {
            var serialize = _cache.GetOrAdd(operation.GetType(), SlowCompileSerializeProperties);
            serialize(operation, writer);
        }

        private SerializePropertiesAction SlowCompileSerializeProperties(Type operationType) {
            var operation = Expression.Parameter(typeof(IOperation), "operation");
            var writer = Expression.Parameter(typeof(IFastJsonWriter), "writer");
            var statements = operationType
                .GetProperties()
                .Where(p => !SlowShouldSkip(p))
                .OrderBy(p => p.Name)
                .Select(p => SlowExpressSerializeProperty(operation, p, writer));

            return Expression.Lambda<SerializePropertiesAction>(
                Expression.Block(statements),
                operation, writer
            ).Compile();
        }

        private Expression SlowExpressSerializeProperty(ParameterExpression operation, PropertyInfo property, ParameterExpression writer) {
            // MemberInfo.DeclaringType is null only on global module methods.
            var propertyValue = Expression.Property(Expression.Convert(operation, property.DeclaringType!), property);

            if (property.PropertyType.GetTypeInfo().IsGenericTypeDefinedAs(typeof(Optional<>))) {
                return Expression.Condition(
                    Expression.Property(propertyValue, nameof(Optional<object>.HasValue)),
                    SlowExpressWriteNameAndValue(
                        property.Name,
                        Expression.Property(propertyValue, nameof(Optional<object>.Value)),
                        writer
                    ),
                    Expression.Empty()
                );
            }

            if (property.PropertyType == typeof(bool)) {
                return Expression.Condition(
                    propertyValue,
                    SlowExpressWriteNameAndValue(property.Name, Expression.Constant(true), writer),
                    Expression.Empty()
                );
            }

            return SlowHandleNulls(
                propertyValue,
                SlowExpressWriteNameAndValue(property.Name, propertyValue, writer),
                Expression.Empty()
            );
        }

        private Expression SlowExpressWriteNameAndValue(string name, Expression value, ParameterExpression writer) {
            var valueToWrite = SlowGetValueToWrite(value);
            return Expression.Block(
                Expression.Call(writer, nameof(IFastJsonWriter.WritePropertyName), typeArguments: null, Expression.Constant(name)),
                Expression.Call(writer, nameof(IFastJsonWriter.WriteValue), typeArguments: null, valueToWrite)
            );
        }

        private static readonly Expression Skipped = Expression.Constant("<skipped>");
        private Expression SlowGetValueToWrite(Expression value) {
            var type = value.Type.GetTypeInfo();
            if (type.IsAssignableTo<IEnumerable>() || type.IsAssignableTo<SyntaxNode>())
                return Skipped;

            if (!DirectlyWritableTypes.Contains(value.Type)) {
                return SlowHandleNulls(
                    value,
                    Expression.Call(value, ObjectToString),
                    Expression.Constant(null, typeof(string))
                );
            }

            return value;
        }

        private Expression SlowHandleNulls(Expression value, Expression ifNotNull, Expression ifNull) {
            if (value.Type.IsValueType)
                return ifNotNull;

            return Expression.Condition(
                Expression.ReferenceNotEqual(value, Expression.Constant(null, value.Type)),
                ifNotNull,
                ifNull
            );
        }

        private bool SlowShouldSkip(PropertyInfo property) {
            return property.Name == nameof(IOperation.Language)
                || property.Name == nameof(IOperation.Kind)
                || property.Name == nameof(IOperation.Parent)
                #pragma warning disable CS0618 // Type or member is obsolete
                || property.Name == nameof(IOperation.Children)
                #pragma warning restore CS0618 // Type or member is obsolete
                || property.Name == nameof(IOperation.ChildOperations)
                || property.Name == nameof(IOperation.Syntax)
                || property.PropertyType.IsAssignableTo<IOperation>()
                || property.PropertyType.IsAssignableTo<IEnumerable<IOperation>>();
        }
    }
}
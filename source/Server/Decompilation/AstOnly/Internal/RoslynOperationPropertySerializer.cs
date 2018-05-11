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
        private static readonly MethodInfo ObjectToString = typeof(object).GetMethod(nameof(ToString));
        private static readonly HashSet<Type> DirectlyWritableTypes = typeof(IFastJsonWriter)
            .GetMethods()
            .Where(m => m.Name == nameof(IFastJsonWriter.WriteValue))
            .Select(m => m.GetParameters()[0].ParameterType)
            .ToSet();

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
            var propertyValue = Expression.Property(Expression.Convert(operation, property.DeclaringType), property);
            if (property.PropertyType.IsGenericTypeDefinedAs(typeof(Optional<>))) {
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

            if (!property.PropertyType.IsValueType) {
                return Expression.Condition(
                    Expression.ReferenceNotEqual(propertyValue, Expression.Constant(null, propertyValue.Type)),
                    SlowExpressWriteNameAndValue(property.Name, propertyValue, writer),
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

            return SlowExpressWriteNameAndValue(property.Name, propertyValue, writer);
        }

        private Expression SlowExpressWriteNameAndValue(string name, Expression value, ParameterExpression writer) {
            var valueToWrite = SlowGetValueToWrite(value);
            return Expression.Block(
                Expression.Call(writer, nameof(IFastJsonWriter.WritePropertyName), typeArguments: null, Expression.Constant(name)),
                Expression.Call(writer, nameof(IFastJsonWriter.WriteValue), typeArguments: null, valueToWrite)
            );
        }

        private static readonly Expression Skipped = Expression.Constant("<skipped>");
        private static Expression SlowGetValueToWrite(Expression value) {
            if (value.Type.IsAssignableTo<IEnumerable>() || value.Type.IsAssignableTo<SyntaxNode>())
                return Skipped;

            if (!DirectlyWritableTypes.Contains(value.Type))
                return Expression.Call(value, ObjectToString);

            return value;
        }

        private bool SlowShouldSkip(PropertyInfo property) {
            return property.Name == nameof(IOperation.Language)
                || property.Name == nameof(IOperation.Kind)
                || property.Name == nameof(IOperation.Parent)
                || property.Name == nameof(IOperation.Children)
                || property.Name == nameof(IOperation.Syntax)
                || property.PropertyType.IsAssignableTo<IOperation>()
                || property.PropertyType.IsAssignableTo<IEnumerable<IOperation>>();
        }
    }
}

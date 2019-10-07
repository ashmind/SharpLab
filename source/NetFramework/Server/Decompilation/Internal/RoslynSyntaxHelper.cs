using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using AshMind.Extensions;
using Microsoft.CodeAnalysis;

namespace SharpLab.Server.Decompilation.Internal {
    public static class RoslynSyntaxHelper {
        // ReSharper disable  HeapView.ObjectAllocation
        // ReSharper disable HeapView.ObjectAllocation.Evident
        private static readonly IReadOnlyDictionary<int, string> KindNames = new[] {
            typeof(Microsoft.CodeAnalysis.CSharp.SyntaxKind),
            typeof(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind),
        }.SelectMany(t => Enum.GetValues(t).Cast<Enum>())
         .Select(e => (name: e.ToString("G"), value: ((IConvertible)e).ToInt32(null)))
         .Distinct()
         .ToDictionary(t => t.value, t => t.name);

        private static readonly ConcurrentDictionary<Type, Lazy<Func<SyntaxNode, SyntaxNode, string>>> CompiledSyntaxNodeGetParentPropertyName 
            = new ConcurrentDictionary<Type, Lazy<Func<SyntaxNode, SyntaxNode, string>>>();
        private static readonly ConcurrentDictionary<Type, Lazy<Func<SyntaxToken, SyntaxNode, string>>> CompiledSyntaxTokenGetParentPropertyName
            = new ConcurrentDictionary<Type, Lazy<Func<SyntaxToken, SyntaxNode, string>>>();
        // ReSharper restore HeapView.ObjectAllocation
        // ReSharper restore HeapView.ObjectAllocation.Evident

        public static string GetKindName(int rawKind) {
            return KindNames[rawKind];
        }

        public static string? GetParentPropertyName(SyntaxToken token) {
            return GetParentPropertyName(token, token.Parent, CompiledSyntaxTokenGetParentPropertyName);
        }

        public static string? GetParentPropertyName(SyntaxNode node) {
            return GetParentPropertyName(node, node.Parent, CompiledSyntaxNodeGetParentPropertyName);
        }

        private static string? GetParentPropertyName<T>(T value, SyntaxNode parent, ConcurrentDictionary<Type, Lazy<Func<T, SyntaxNode, string>>> compiledCache) {
            if (parent == null)
                return null;
            var compiled = compiledCache.GetOrAdd(
                parent.GetType(),
                t => new Lazy<Func<T, SyntaxNode, string>>(() => SlowCompileGetParentPropertyName<T>(t), LazyThreadSafetyMode.ExecutionAndPublication)
            ).Value;
            return compiled(value, parent);
        }

        // ReSharper disable HeapView.ObjectAllocation.Evident
        private static Func<T, SyntaxNode, string> SlowCompileGetParentPropertyName<T>(Type parentSyntaxType) {
            var value = Expression.Parameter(typeof(T));
            var parent = Expression.Parameter(typeof(SyntaxNode), "parent");
            var parentTyped = Expression.Variable(parentSyntaxType);

            var end = Expression.Label(typeof(string));
            var statements = new List<Expression> {
                Expression.Assign(parentTyped, Expression.Convert(parent, parentSyntaxType))
            };
            foreach (var property in parentSyntaxType.GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
                if (property.Name == nameof(SyntaxNode.Parent))
                    continue;
                if (!property.PropertyType.GetTypeInfo().IsSameAsOrSubclassOf<T>())
                    continue;
                var propertyEqualsNode = Expression.Equal(Expression.Property(parentTyped, property), value);
                statements.Add(Expression.IfThen(propertyEqualsNode, Expression.Return(end, Expression.Constant(property.Name))));
            }
            statements.Add(Expression.Label(end, Expression.Constant(null, typeof(string))));
            return Expression
                .Lambda<Func<T, SyntaxNode, string>>(Expression.Block(new[] { parentTyped }, statements), value, parent)
                .Compile();
        }
        // ReSharper restore HeapView.ObjectAllocation.Evident
    }
}

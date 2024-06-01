using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.FSharp.Collections;
using MirrorSharp.Advanced;
using MirrorSharp.FSharp.Advanced;
using SharpLab.Server.Decompilation.Internal;
using FSharp.Compiler.Syntax;
using Range = FSharp.Compiler.Text.Range;

namespace SharpLab.Server.Decompilation.AstOnly;

public class FSharpAstTarget : IAstTarget {
    private delegate void SerializeChildAction<T>(T item, IFastJsonWriter writer, string parentPropertyName, ref bool childrenStarted, IFSharpSession session);
    private delegate void SerializeChildrenAction(object parent, IFastJsonWriter writer, ref bool childrenStarted, IFSharpSession session);
    private delegate Range GetRangeFunc(object target);

    private static readonly string SyntaxNamespace = typeof(Ident).Namespace!;
    private static readonly Lazy<IReadOnlyList<Type>> TopLevelAstTypes = new(
        () => typeof(Ident).Assembly.GetTypes().Where(t => t.Namespace == SyntaxNamespace && !t.IsNested).ToList(),
        LazyThreadSafetyMode.ExecutionAndPublication
    );

    private static readonly ConcurrentDictionary<Type, Lazy<SerializeChildrenAction>> ChildrenSerializers = new();
    private static readonly ConcurrentDictionary<Type, Lazy<GetRangeFunc?>> RangeGetters = new();
    private static readonly Lazy<IReadOnlyDictionary<Type, Func<object, string>>> TagNameGetters =
        new(SlowCompileTagNameGetters, LazyThreadSafetyMode.ExecutionAndPublication);
    private static readonly Lazy<IReadOnlyDictionary<Type, Func<SynConst, string>>> ConstValueGetters =
        new(SlowCompileConstValueGetters, LazyThreadSafetyMode.ExecutionAndPublication);
    private static readonly Lazy<IReadOnlyDictionary<Type, string>> AstTypeNames =
        new(SlowCollectAstTypeNames, LazyThreadSafetyMode.ExecutionAndPublication);

    private static class Methods {
        // ReSharper disable MemberHidesStaticFromOuterClass
        // ReSharper disable HeapView.DelegateAllocation
        public static readonly MethodInfo SerializeNode =
            ((SerializeChildAction<object>)FSharpAstTarget.SerializeNode).Method.GetGenericMethodDefinition();
        public static readonly MethodInfo SerializeList =
            ((SerializeChildAction<FSharpList<object>>)FSharpAstTarget.SerializeList).Method.GetGenericMethodDefinition();
        public static readonly MethodInfo SerializeIdent =
            ((SerializeChildAction<Ident>)FSharpAstTarget.SerializeIdent).Method;
        public static readonly MethodInfo SerializeIdentList =
            ((SerializeChildAction<FSharpList<Ident>>)FSharpAstTarget.SerializeIdentList).Method;
        public static readonly MethodInfo SerializeEnum =
            ((SerializeChildAction<int>)FSharpAstTarget.SerializeEnum).Method.GetGenericMethodDefinition();
        // ReSharper restore HeapView.DelegateAllocation
        // ReSharper restore MemberHidesStaticFromOuterClass
    }

    private static class EnumCache<TEnum>
        where TEnum : struct, IFormattable {
        public static readonly IReadOnlyDictionary<TEnum, string> Strings = Enum.GetValues(typeof(TEnum)).Cast<TEnum>().ToDictionary(e => e, e => e.ToString("G", null));
    }

    public Task<object> GetAstAsync(IWorkSession session, CancellationToken cancellationToken) {
        var parseResult = session.FSharp().GetLastParseResults();
        if (parseResult == null)
            throw new InvalidOperationException("Current session does not include F# parse results yet.");
        return Task.FromResult((object)parseResult.ParseTree);
    }

    public void SerializeAst(object ast, IFastJsonWriter writer, IWorkSession session) {
        var root = ((ParsedInput.ImplFile)ast).Item;
        writer.WriteStartArray();
        var childrenStarted = true;
        SerializeNode(root, writer, null, ref childrenStarted, session.FSharp());
        writer.WriteEndArray();
    }

    private static void SerializeNode<T>(T node, IFastJsonWriter writer, string? parentPropertyName, ref bool parentChildrenStarted, IFSharpSession session)
        where T: notnull
    {
        EnsureChildrenStarted(ref parentChildrenStarted, writer);
        writer.WriteStartObject();
        var nodeType = node.GetType();
        writer.WriteProperty("kind", AstTypeNames.Value[nodeType]);
        if (parentPropertyName != null)
            writer.WriteProperty("property", parentPropertyName);

        if (node is SynConst @const) {
            writer.WriteProperty("type", "token");
            if (@const is SynConst.String @string) {
                writer.WritePropertyName("value");
                writer.WriteValueFromParts("\"", @string.text, "\"");
            }
            else if (@const is SynConst.Char @char) {
                writer.WritePropertyName("value");
                writer.WriteValueFromParts("'", @char.Item, "'");
            }
            else {
                if (ConstValueGetters.Value.TryGetValue(nodeType, out var getter)) {
                    writer.WritePropertyName("value");
                    writer.WriteValue(getter(@const));
                }
            }
        }
        else {
            writer.WriteProperty("type", nodeType.IsValueType ? "value" : "node");
            var tagName = GetTagName(node);
            if (tagName != null)
                writer.WriteProperty("value", tagName);
        }
        var rangeGetter = GetRangeGetter(nodeType);
        if (rangeGetter != null)
            SerializeRangeProperty(rangeGetter(node), writer, session);

        var childrenStarted = false;
        GetChildrenSerializer(nodeType).Invoke(node, writer, ref childrenStarted, session);
        EnsureChildrenEnded(childrenStarted, writer);
        writer.WriteEndObject();
    }

    private static void SerializeList<T>(FSharpList<T> list, IFastJsonWriter writer, string? parentPropertyName, ref bool parentChildrenStarted, IFSharpSession session)
        where T: notnull
    {
        foreach (var item in list) {
            SerializeNode(item, writer, null /* UI does not support list property names at the moment */, ref parentChildrenStarted, session);
        }
    }

    private static void SerializeIdent(Ident ident, IFastJsonWriter writer, string? parentPropertyName, ref bool parentChildrenStarted, IFSharpSession session) {
        EnsureChildrenStarted(ref parentChildrenStarted, writer);
        writer.WriteStartObject();
        writer.WriteProperty("type", "token");
        writer.WriteProperty("kind", "Ident");
        if (parentPropertyName != null)
            writer.WriteProperty("property", parentPropertyName);
        writer.WriteProperty("value", ident.idText);
        SerializeRangeProperty(ident.idRange, writer, session);
        writer.WriteEndObject();
    }

    private static void SerializeIdentList(FSharpList<Ident> list, IFastJsonWriter writer, string? parentPropertyName, ref bool parentChildrenStarted, IFSharpSession session) {
        foreach (var ident in list) {
            SerializeIdent(ident, writer, parentPropertyName, ref parentChildrenStarted, session);
        }
    }

    private static void SerializeEnum<TEnum>(TEnum value, IFastJsonWriter writer, string? parentPropertyName, ref bool parentChildrenStarted, IFSharpSession session)
        where TEnum : struct, IFormattable {
        EnsureChildrenStarted(ref parentChildrenStarted, writer);
        if (parentPropertyName != null) {
            writer.WriteStartObject();
            writer.WriteProperty("type", "value");
            writer.WriteProperty("property", parentPropertyName);
            writer.WriteProperty("value", EnumCache<TEnum>.Strings[value]);
            writer.WriteEndObject();
        }
        else {
            writer.WriteValue(EnumCache<TEnum>.Strings[value]);
        }
    }

    private static void SerializeRangeProperty(Range range, IFastJsonWriter writer, IFSharpSession session) {
        writer.WritePropertyName("range");
        var startOffset = session.ConvertToOffset(range.StartLine, range.StartColumn);
        var endOffset = session.ConvertToOffset(range.EndLine, range.EndColumn);
        writer.WriteValueFromParts(startOffset, '-', endOffset);
    }

    private static void EnsureChildrenStarted(ref bool childrenStarted, IFastJsonWriter writer) {
        if (childrenStarted)
            return;
        writer.WritePropertyStartArray("children");
        childrenStarted = true;
    }

    private static void EnsureChildrenEnded(bool childrenStarted, IFastJsonWriter writer) {
        if (!childrenStarted)
            return;
        writer.WriteEndArray();
    }

    private static SerializeChildrenAction GetChildrenSerializer(Type type) {
        if (!ChildrenSerializers.TryGetValue(type, out var lazySerialize)) {
            lazySerialize = ChildrenSerializers.GetOrAdd(
                type,
                t => new(() => SlowCompileChildrenSerializer(t), LazyThreadSafetyMode.ExecutionAndPublication)
            );
        }

        return lazySerialize.Value;
    }

    private static SerializeChildrenAction SlowCompileChildrenSerializer(Type type) {
        var nodeAsObject = Expression.Parameter(typeof(object));
        var writer = Expression.Parameter(typeof(IFastJsonWriter));
        var refChildrenStarted = Expression.Parameter(typeof(bool).MakeByRefType());
        var session = Expression.Parameter(typeof(IFSharpSession));

        var node = Expression.Variable(type);
        var body = new List<Expression> {
            Expression.Assign(node, Expression.Convert(nodeAsObject, type))
        };

        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public)) {
            if (ShouldSkipNodeProperty(type, property))
                continue;
            var propertyType = property.PropertyType;
            var method = SlowGetMethodToSerialize(propertyType);
            if (method == null)
                continue;

            var propertyName = property.Name;
            if (Regex.IsMatch(propertyName, @"^Item\d*$"))
                propertyName = null;
            body.Add(Expression.Call(method, Expression.Property(node, property), writer, Expression.Constant(propertyName, typeof(string)), refChildrenStarted, session));
        }

        return Expression.Lambda<SerializeChildrenAction>(
            Expression.Block(new[] { node }, body),
            nodeAsObject, writer, refChildrenStarted, session
        ).Compile();
    }

    private static MethodInfo? SlowGetMethodToSerialize(Type propertyType) {
        if (propertyType == typeof(Ident))
            return Methods.SerializeIdent;

        if (propertyType == typeof(FSharpList<Ident>))
            return Methods.SerializeIdentList;

        if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(FSharpList<>)) {
            var elementType = propertyType.GetGenericArguments()[0];
            if (!IsNodeType(elementType))
                return null;
            return Methods.SerializeList.MakeGenericMethod(elementType);
        }

        if (!IsNodeType(propertyType))
            return null;

        if (propertyType.IsEnum)
            return Methods.SerializeEnum.MakeGenericMethod(propertyType);

        return Methods.SerializeNode.MakeGenericMethod(propertyType);
    }

    private static GetRangeFunc? GetRangeGetter(Type type) {
        return RangeGetters.GetOrAdd(
            type,
            t => new Lazy<GetRangeFunc?>(() => CompileRangeGetter(t), LazyThreadSafetyMode.ExecutionAndPublication)
        ).Value;
    }

    private static GetRangeFunc? CompileRangeGetter(Type type) {
        var rangeProperty = type.GetProperty("Range");
        if (rangeProperty == null)
            return null;

        var nodeAsObject = Expression.Parameter(typeof(object));
        var body = Expression.Property(Expression.Convert(nodeAsObject, type), rangeProperty);

        return Expression.Lambda<GetRangeFunc>(body, [nodeAsObject]).Compile();
    }

    private static bool ShouldSkipNodeProperty(Type type, PropertyInfo property) {
        return false;
        //return (type == typeof(LongIdentWithDots) && property.Name == nameof(LongIdentWithDots.id));
    }

    private static bool IsNodeType(Type type) {
        return type.Namespace == SyntaxNamespace
            && type != typeof(QualifiedNameOfFile)
            && type != typeof(SynModuleOrNamespaceKind)
            && !(type.Name.StartsWith("SequencePoint"));
    }

    private static string? GetTagName(object node) {
        return TagNameGetters.Value.TryGetValue(node.GetType(), out var getter)
             ? getter.Invoke(node)
             : null;
    }

    private static IReadOnlyDictionary<Type, Func<object, string>> SlowCompileTagNameGetters() {
        var getters = new Dictionary<Type, Func<object, string>>();
        void SlowCompileAndCollectRecursive(Type astType) {
            foreach (var nested in astType.GetNestedTypes()) {
                if (nested.Name == "Tags") {
                    getters.Add(astType, SlowCompileTagNameGetter(astType, nested));
                    continue;
                }
                SlowCompileAndCollectRecursive(nested);
            }
        }

        foreach (var topLevel in TopLevelAstTypes.Value) {
            SlowCompileAndCollectRecursive(topLevel);
        }
        return getters;
    }

    private static Func<object, string> SlowCompileTagNameGetter(Type astType, Type tagsType) {
        var tagMap = tagsType
            .GetFields()
            .OrderBy(f => (int)f.GetValue(null)!)
            .Select(f => f.Name)
            .ToArray();
        var nodeUntyped = Expression.Parameter(typeof(object));
        var tagGetter = Expression.Lambda<Func<object, int>>(
            Expression.Property(Expression.Convert(nodeUntyped, astType), "Tag"),
            nodeUntyped
        ).Compile();
        return instance => tagMap[tagGetter(instance)];
    }

    private static IReadOnlyDictionary<Type, Func<SynConst, string>> SlowCompileConstValueGetters() {
        var getters = new Dictionary<Type, Func<SynConst, string>>();
        foreach (var type in typeof(SynConst).GetNestedTypes()) {
            if (type.BaseType != typeof(SynConst))
                continue;

            var valueProperty = type.GetProperty("Item");
            if (valueProperty == null)
                continue;

            var toString = valueProperty.PropertyType.GetMethod("ToString", Type.EmptyTypes)!;
            var constUntyped = Expression.Parameter(typeof(SynConst));
            getters.Add(type, Expression.Lambda<Func<SynConst, string>>(
                Expression.Call(Expression.Property(Expression.Convert(constUntyped, type), valueProperty), toString),
                constUntyped
            ).Compile());
        }
        return getters;
    }

    private static IReadOnlyDictionary<Type, string> SlowCollectAstTypeNames() {
        var results = new Dictionary<Type, string>();
        void CollectRecusive(IEnumerable<Type> astTypes, string parentPrefix) {
            foreach (var astType in astTypes) {
                var name = parentPrefix + astType.Name;
                var prefix = name + ".";
                results.Add(astType, name);
                CollectRecusive(astType.GetNestedTypes(), prefix);
            }
        }

        CollectRecusive(TopLevelAstTypes.Value, "");
        return results;
    }

    public IReadOnlyCollection<string> SupportedLanguageNames { get; } = new[] { "F#" };
}
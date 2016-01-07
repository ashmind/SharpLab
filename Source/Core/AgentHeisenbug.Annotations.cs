using System;
using System.Collections.Generic;
using System.Linq;

// dogfooding my future R# plugin

// ReSharper disable once CheckNamespace
namespace JetBrains.Annotations {
    [ThreadSafe]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method | AttributeTargets.Struct | AttributeTargets.GenericParameter, AllowMultiple = false, Inherited = false)]
    public class ThreadSafeAttribute : Attribute { }

    [ThreadSafe]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method | AttributeTargets.Struct | AttributeTargets.GenericParameter, AllowMultiple = false, Inherited = false)]
    public class ReadOnlyAttribute : Attribute { }
}

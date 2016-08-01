using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;

namespace TryRoslyn.Core.Processing.Languages.Internal {
    public class CSharpFeatureDiscovery : IFeatureDiscovery {
        public IReadOnlyCollection<string> SlowDiscoverAll() {
            var assembly = typeof(CSharpCompilation).Assembly;
            var messageIdType = assembly.GetType("Microsoft.CodeAnalysis.CSharp.MessageID");
            if (messageIdType == null)
                return new string[0];

            var messageIdExtensionsType = assembly.GetType("Microsoft.CodeAnalysis.CSharp.MessageIDExtensions");
            if (messageIdExtensionsType == null)
                return new string[0];

            var requiredFeature = messageIdExtensionsType.GetMethod("RequiredFeature", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (requiredFeature == null)
                return new string[0];

            var messageIds = Enum.GetValues(messageIdType).Cast<object>();
            return messageIds
                .Select(id => (string)requiredFeature.Invoke(null, new[] {id}))
                .Where(f => f != null)
                .Distinct()
                .ToList();
        }
    }
}

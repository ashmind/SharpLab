using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;

namespace TryRoslyn.Core.Processing.Internal {
    public class CSharpFeatures : ICSharpFeatures {
        private readonly Lazy<IReadOnlyCollection<string>> _features;

        public CSharpFeatures() {
            _features = new Lazy<IReadOnlyCollection<string>>(
                DiscoverAllUncached,
                LazyThreadSafetyMode.ExecutionAndPublication
            );
        }

        private IReadOnlyCollection<string> DiscoverAllUncached() {
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

        public IReadOnlyCollection<string> DiscoverAll() => _features.Value;
    }
}

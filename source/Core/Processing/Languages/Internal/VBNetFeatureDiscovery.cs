using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.VisualBasic;

namespace TryRoslyn.Core.Processing.Languages.Internal {
    public class VBNetFeatureDiscovery : IFeatureDiscovery {
        public IReadOnlyCollection<string> SlowDiscoverAll() {
            var assembly = typeof(VisualBasicCompilation).Assembly;
            var featureType = assembly.GetType("Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax.Feature");
            if (featureType == null)
                return new string[0];

            var featureExtensionsType = assembly.GetType("Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax.FeatureExtensions");
            if (featureExtensionsType == null)
                return new string[0];

            var getFeatureFlag = featureExtensionsType.GetMethod("GetFeatureFlag", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (getFeatureFlag == null)
                return new string[0];

            var features = Enum.GetValues(featureType).Cast<object>();
            return features
                .Select(f => (string)getFeatureFlag.Invoke(null, new[] { f }))
                .Where(f => f != null)
                .Distinct()
                .ToList();
        }
    }
}

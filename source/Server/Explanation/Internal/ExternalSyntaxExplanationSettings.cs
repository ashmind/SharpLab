using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpLab.Server.Explanation.Internal {
    public class ExternalSyntaxExplanationSettings {
        public ExternalSyntaxExplanationSettings(Uri sourceUrl, TimeSpan updatePeriod) {
            SourceUrl = sourceUrl;
            UpdatePeriod = updatePeriod;
        }

        public Uri SourceUrl { get; }
        public TimeSpan UpdatePeriod { get; }
    }
}

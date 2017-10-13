using System.Collections.Generic;

namespace IL.Syntax {
    public class MultipartIdentifier {
        public MultipartIdentifier(IList<string> parts) {
            Parts = parts;
        }

        public IList<string> Parts { get; }

        public string ToUnescapedString() {
            return string.Join(".", Parts);
        }
    }
}

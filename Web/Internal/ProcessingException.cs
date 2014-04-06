using System;
using System.Runtime.Serialization;

namespace TryRoslyn.Web.Internal {
    [Serializable]
    public class ProcessingException : Exception {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public ProcessingException() {
        }

        public ProcessingException(string message) : base(message) {
        }

        public ProcessingException(string message, Exception inner) : base(message, inner) {
        }

        protected ProcessingException(
            SerializationInfo info,
            StreamingContext context) : base(info, context) {
        }
    }
}
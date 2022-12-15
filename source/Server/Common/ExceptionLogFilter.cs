using MirrorSharp.Advanced;
using System;
using System.Net.WebSockets;

namespace SharpLab.Server.Common {
    public class ExceptionLogFilter : IExceptionLogFilter {
        public bool ShouldLog(Exception exception, IWorkSession session) {
            // Note/TODO: need to see if OperationCanceledException can be avoided
            // https://github.com/ashmind/SharpLab/issues/617
            if (exception is WebSocketException or OperationCanceledException)
                return false;

            if (session.LanguageName == LanguageNames.IL && session.GetText().Contains(".emitbyte") && exception is BadImageFormatException)
                return false; // 🤷 emit byte, break assembly

            return true;
        }
    }
}
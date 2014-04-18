using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace TryRoslyn.Web.Formatting {
    public class CodeMediaTypeFormatter : MediaTypeFormatter {
        public CodeMediaTypeFormatter() {
            SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/plain"));
            SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/x-csharp"));
        }

        public async override Task<object> ReadFromStreamAsync(Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger) {
            using (var reader = new StreamReader(readStream)) {
                return await reader.ReadToEndAsync();
            }
        }

        public async override Task WriteToStreamAsync(Type type, object value, Stream writeStream, HttpContent content, TransportContext transportContext, CancellationToken cancellationToken) {
            using (var writer = new StreamWriter(writeStream)) {
                await writer.WriteAsync((string)value);
            }
        }

        public override bool CanReadType(Type type) {
            return type == typeof(string);
        }

        public override bool CanWriteType(Type type) {
            return type == typeof(string);
        }
    }
}
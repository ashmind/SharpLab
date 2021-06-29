using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SharpLab.Container.Manager.Internal;

namespace SharpLab.Container.Manager.Endpoints {
    public class StatusEndpoint {
        private readonly ReadOnlyMemory<byte> _okBytes = new(Encoding.UTF8.GetBytes("OK"));
        private readonly ContainerPool _containerPool;

        public StatusEndpoint(ContainerPool containerPool) {
            _containerPool = containerPool;
        }

        public Task ExecuteAsync(HttpContext context) {
            context.Response.ContentType = "text/plain";
            if (_containerPool.ContainerPreallocationFailingSince < DateTimeOffset.Now.AddMinutes(-5)) {
                context.Response.StatusCode = 543; // custom code
                return context.Response.WriteAsync($"🪦: Unable to allocate container since {_containerPool.ContainerPreallocationFailingSince:HH:mm:ss dd MMM yyyy}:\n{_containerPool.LastContainerPreallocationException}", context.RequestAborted);
            }

            return context.Response.BodyWriter.WriteAsync(_okBytes, context.RequestAborted).AsTask();
        }
    }
}

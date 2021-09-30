using System.Threading.Tasks;
using Fragile;
using Microsoft.AspNetCore.Http;

namespace SharpLab.Container.Manager.Endpoints {
    public class DebugEndpoint {
        private static IProcessContainer? _container;
        private readonly IProcessRunner _runner;

        public DebugEndpoint(IProcessRunner runner) {
            _runner = runner;
        }

        public Task CreateAsync(HttpContext context) {
            if (_container != null) {
                if (!_container.Process.HasExited)
                    return context.Response.WriteAsync($"DEBUG : CONTAINER EXISTS : {_container.Process.Id}");

                _container.Dispose();
                _container = null;
            }

            _container = _runner.StartProcess(DEBUG_suspended: true);
            return context.Response.WriteAsync($"DEBUG : CONTAINER CREATED : {_container.Process.Id}");
        }
    }
}

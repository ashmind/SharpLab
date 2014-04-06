using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using TryRoslyn.Web.Internal;

namespace TryRoslyn.Web.Controllers {
    public class RoslynController : ApiController {
        private readonly CompilationService _service;

        public RoslynController() : this(new CompilationService()) {
        }

        public RoslynController(CompilationService service) {
            _service = service;
        }

        [HttpPost]
        [Route("api/compilation")]
        public HttpResponseMessage Compilation([FromBody] string code) {
            try {
                var result = _service.CompileThenDecompile(code);
                return Request.CreateResponse(HttpStatusCode.OK, result, "text/x-csharp");
            }
            catch (CompilationException ex) {
                return Request.CreateResponse((HttpStatusCode)422, ex.Message, "text/plain");
            }
        }
    }
}

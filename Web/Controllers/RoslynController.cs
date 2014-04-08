using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Microsoft.CodeAnalysis;
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
        public object Compilation([FromBody] string code) {
            var result = _service.Process(code);
            return new {
                success = result.IsSuccess,
                result.Decompiled,
                errors   = result.GetDiagnostics(DiagnosticSeverity.Error).Select(d => d.ToString()),
                warnings = result.GetDiagnostics(DiagnosticSeverity.Warning).Select(d => d.ToString())
            };
        }
    }
}

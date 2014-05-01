using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using Microsoft.CodeAnalysis;
using TryRoslyn.Core;

namespace TryRoslyn.Web.Controllers {
    // routes are a mess at the moment
    public class RoslynController : ApiController {
        private readonly ICodeProcessorManager _processorManager;
        private readonly IBranchProvider _branchProvider;

        public RoslynController(ICodeProcessorManager processorManager, IBranchProvider branchProvider) {
            _processorManager = processorManager;
            _branchProvider = branchProvider;
        }

        [HttpGet]
        [Route("api/branches")]
        public IEnumerable<string> Branches() {
            return _branchProvider.GetBranchNames();
        }
        
        [HttpPost]
        [Route("api/compilation")]
        public object Compilation([FromBody] string code, string branch = null) {
            var processor = branch != null 
                          ? _processorManager.GetBranchProcessor(branch)
                          : _processorManager.DefaultProcessor;

            var result = processor.Process(code);
            return new {
                success = result.IsSuccess,
                //result.SyntaxTree,
                result.Decompiled,
                errors   = result.GetDiagnostics(DiagnosticSeverity.Error).Select(d => d.ToString()),
                warnings = result.GetDiagnostics(DiagnosticSeverity.Warning).Select(d => d.ToString())
            };
        }
    }
}

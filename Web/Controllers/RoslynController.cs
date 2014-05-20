using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using Microsoft.CodeAnalysis;
using TryRoslyn.Core;
using TryRoslyn.Web.Models;

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
        public IEnumerable<BranchInfo> Branches() {
            return _branchProvider.GetBranches();
        }
        
        [HttpPost]
        [Route("api/compilation")]
        public object Compilation([FromBody] CompilationArguments arguments) {
            var processor = arguments.Branch != null
                          ? _processorManager.GetBranchProcessor(arguments.Branch)
                          : _processorManager.DefaultProcessor;

            var result = processor.Process(
              arguments.Code, arguments.Mode == CompilationMode.Script, arguments.Optimizations);

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

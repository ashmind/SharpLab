using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Cors;
using Microsoft.CodeAnalysis;
using TryRoslyn.Core;
using TryRoslyn.Web.Models;

namespace TryRoslyn.Web.Controllers {
    // routes are a mess at the moment
    public class RoslynController : ApiController {
        private readonly ICodeProcessor _processor;

        public RoslynController(ICodeProcessor processor) {
            _processor = processor;
        }

        [HttpPost]
        [Route("api/compilation")]
        public object Compilation([FromBody] CompilationArguments arguments) {
            var result = _processor.Process(arguments.Code, new ProcessingOptions {
                SourceLanguage = arguments.SourceLanguage,
                TargetLanguage = arguments.TargetLanguage,
                ScriptMode = arguments.Mode == CompilationMode.Script,
                OptimizationsEnabled = arguments.OptimizationsEnabled
            });

            return new {
                success = result.IsSuccess,
                //result.SyntaxTree,
                result.Decompiled,
                errors   = result.GetDiagnostics(DiagnosticSeverity.Error),
                warnings = result.GetDiagnostics(DiagnosticSeverity.Warning)
            };
        }
    }
}

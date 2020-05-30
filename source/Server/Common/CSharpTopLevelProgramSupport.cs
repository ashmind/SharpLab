using System;
using System.Collections.Generic;
using System.Linq;
using AshMind.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MirrorSharp.Advanced;
using SharpLab.Server.Common;
using SharpLab.Server.MirrorSharp;

namespace SharpLab.Server.Compilation {
    using static OutputKind;

    public class CSharpTopLevelProgramSupport : ICSharpTopLevelProgramSupport {
        // not all branches have this yet, so we can't use the normal enum value
        private static readonly SyntaxKind? GlobalStatement = (SyntaxKind?)typeof(SyntaxKind).GetField("GlobalStatement")?.GetValue(null);

        public void UpdateOutputKind(IWorkSession session, IList<Diagnostic>? diagnostics = null) {
            if (session.LanguageName != LanguageNames.CSharp)
                return;

            if (GlobalStatement == null)
                return; // this branch does not support global statements

            if (session.GetTargetName() == TargetNames.Run)
                return; // must always use executable mode for Run

            if (!session.Roslyn.Project.Documents.Single().TryGetSyntaxRoot(out var syntaxRoot)) {
                if (diagnostics != null) // we must update now, otherwise diagnostics will be incorrect
                    throw new InvalidOperationException("Syntax root was not cached.");
                return; // will update later
            }

            // We can't always mark it as executable as it would require Main() to be present,
            // so we toggle conditionally.
            var shouldBeExecutable = syntaxRoot.ChildNodes().Any(n => n.IsKind(GlobalStatement.Value));
            var isExecutable = session.Roslyn.Project.CompilationOptions?.OutputKind == ConsoleApplication;

            if (isExecutable == shouldBeExecutable)
                return;

            diagnostics?.RemoveWhere(d => d.Id == (shouldBeExecutable ? "CS9004" : "CS5001"));

            var project = session.Roslyn.Project;
            session.Roslyn.Project = project.WithCompilationOptions(
                project.CompilationOptions!.WithOutputKind(shouldBeExecutable ? ConsoleApplication : DynamicallyLinkedLibrary)
            );
        }
    }
}

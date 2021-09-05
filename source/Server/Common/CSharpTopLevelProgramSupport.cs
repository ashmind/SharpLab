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
    using LanguageNames = Common.LanguageNames;

    public class CSharpTopLevelProgramSupport : ICSharpTopLevelProgramSupport {
        private static class DiagnosticIds {
            // "Program using top-level statements must be an executable."
            public const string TopLevelNotInExecutable = "CS8805"; // final code
            public const string TopLevelNotInExecutableOld = "CS9004"; // old code in feature branch

            // "Program does not contain a static 'Main' method suitable for an entry point"
            public const string NoStaticMain = "CS8805";
        }

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

            diagnostics?.RemoveWhere(
                d => shouldBeExecutable
                   ? (d.Id == DiagnosticIds.TopLevelNotInExecutable || d.Id == DiagnosticIds.TopLevelNotInExecutableOld)
                   : (d.Id == DiagnosticIds.NoStaticMain)
            );

            var project = session.Roslyn.Project;
            session.Roslyn.Project = project.WithCompilationOptions(
                project.CompilationOptions!.WithOutputKind(shouldBeExecutable ? ConsoleApplication : DynamicallyLinkedLibrary)
            );
        }
    }
}

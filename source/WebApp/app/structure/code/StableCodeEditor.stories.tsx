import React from 'react';
import { RecoilRoot } from 'recoil';
import { recoilTestState } from '../../shared/helpers/testing/recoilTestState';
import { LanguageName, LANGUAGE_CSHARP, LANGUAGE_FSHARP, LANGUAGE_IL, LANGUAGE_VB } from '../../shared/languages';
import { languageOptionState } from '../../shared/state/languageOptionState';
import type { Flow } from '../../shared/resultTypes';
import { loadedCodeState } from '../../shared/state/loadedCodeState';
import { darkModeStory } from '../../shared/testing/darkModeStory';
import { CodeEditorProps, StableCodeEditor } from './StableCodeEditor';

export default {
    component: StableCodeEditor,
    excludeStories: /^EXAMPLE_/
};

export const EXAMPLE_CODE_WITH_EXECUTION_FLOW = {
    CODE: `
for (var i = 0; i < 3; i++) {
    Test(i);
}

Test(4);
Test(5);
Test(6);

int Test(int x) {
    return x;
}
    `.trim(),
    FLOW: {
        steps: [
            { line: 1, notes: 'i: 0', jump: true },
            { line: 2, jump: true },
            { line: 9, skipped: false, notes: 'x: 0' },
            { line: 10, jump: true },
            { line: 11, jump: true, notes: 'return: 0' },
            { line: 3 },
            { line: 1, notes: 'i: 1', jump: true },
            { line: 2, jump: true },
            { line: 9, skipped: false, notes: 'x: 1' },
            { line: 10, jump: true },
            { line: 11, jump: true, notes: 'return: 1' },
            { line: 3 },
            { line: 1, notes: 'i: 2', jump: true },
            { line: 2, jump: true },
            { line: 9, skipped: false, notes: 'x: 2' },
            { line: 10, jump: true },
            { line: 11, jump: true, notes: 'return: 2' },
            { line: 3 },
            { line: 1, notes: 'i: 3', jump: true },
            { line: 5, jump: true },
            { line: 9, skipped: false, notes: 'x: 4' },
            { line: 10, jump: true },
            { line: 11, jump: true, notes: 'return: 4' },
            { line: 6, jump: true },
            { line: 9, skipped: false, notes: 'x: 5' },
            { line: 10, jump: true },
            { line: 11, jump: true, notes: 'return: 5' },
            { line: 7, jump: true },
            { line: 9, skipped: false, notes: 'x: 6' },
            { line: 10, jump: true },
            { line: 11, jump: true, notes: 'return: 6' }
        ],
        areas: [
            { type: 'method', startLine: 1, endLine: 7 },
            { type: 'method', startLine: 9, endLine: 11 },
            { type: 'loop', startLine: 1, endLine: 3 }
        ]
    } as Flow
} as const;

type TemplateProps = {
    language: LanguageName;
    loadedCode: string;
    executionFlow?: Flow;
} & Pick<CodeEditorProps, 'initialExecutionFlowSelectRule'>;
// eslint-disable-next-line @typescript-eslint/no-empty-function
const doNothing = () => {};

const Template: React.FC<TemplateProps> = ({ language, loadedCode, executionFlow, initialExecutionFlowSelectRule }) => <>
    <RecoilRoot initializeState={recoilTestState(
        [languageOptionState, language],
        [loadedCodeState, loadedCode]
    )}>
        <StableCodeEditor
            onCodeChange={doNothing}
            onConnectionChange={doNothing}
            onServerError={doNothing}
            onSlowUpdateResult={doNothing}
            onSlowUpdateWait={doNothing}
            executionFlow={executionFlow ?? null}
            initialCached
            initialExecutionFlowSelectRule={initialExecutionFlowSelectRule} />
    </RecoilRoot>
</>;

export const CSharp = () => <Template language={LANGUAGE_CSHARP} loadedCode={`
using System;
public class C {
    public void M() {
        var number = 1;
        var @string = "abc";
    }
}
`.trim()} />;
CSharp.storyName = 'C#';
export const CSharpDarkMode = darkModeStory(CSharp);

export const VisualBasic = () => <Template language={LANGUAGE_VB} loadedCode={`
Imports System
Public Class C
    Public Sub M() {
        Dim number = 1;
        Dim [string] = "abc";
    End Sub
End Class
`.trim()} />;
export const VisualBasicDarkMode = darkModeStory(VisualBasic);

export const FSharp = () => <Template language={LANGUAGE_FSHARP} loadedCode={`
open System;

let number = 1;
let string = "abc";
`.trim()} />;
FSharp.storyName = 'F#';
export const FSharpDarkMode = darkModeStory(FSharp);

export const IL = () => <Template language={LANGUAGE_IL} loadedCode={`
.class public auto ansi abstract sealed beforefieldinit C
    extends System.Object
{
    .method public hidebysig static
        void M () cil managed
    {
        .maxstack 8
        ldc.i4.1
        stloc.0
        ldstr "abc"
        stloc.1
        ret
    }
}
`.trim()} />;
export const ILDarkMode = darkModeStory(IL);

export const ExecutionFlow = () => <Template
    language={LANGUAGE_CSHARP}
    loadedCode={EXAMPLE_CODE_WITH_EXECUTION_FLOW.CODE}
    executionFlow={EXAMPLE_CODE_WITH_EXECUTION_FLOW.FLOW}
/>;
export const ExecutionFlowDarkMode = darkModeStory(ExecutionFlow);

const selectFlowVisit = () => 1;
export const ExecutionFlowWithSelectedVisits = () => <Template
    language={LANGUAGE_CSHARP}
    loadedCode={EXAMPLE_CODE_WITH_EXECUTION_FLOW.CODE}
    executionFlow={EXAMPLE_CODE_WITH_EXECUTION_FLOW.FLOW}
    initialExecutionFlowSelectRule={selectFlowVisit}
/>;
export const ExecutionFlowWithSelectedVisitsDarkMode = darkModeStory(ExecutionFlowWithSelectedVisits);

export const ExecutionFlowException = () => <Template language={LANGUAGE_CSHARP} loadedCode={`
try {
    throw new();
}
catch {
}
`.trim()} executionFlow={{
    steps: [
        { line: 1 },
        { line: 2, exception: 'Exception' },
        { line: 4 },
        { line: 5 }
    ],
    areas: []
}} />;
export const ExecutionFlowExceptionDarkMode = darkModeStory(ExecutionFlowException);
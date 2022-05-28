import React from 'react';
import { RecoilRoot } from 'recoil';
import { DarkModeRoot } from '../../shared/testing/DarkModeRoot';
import { recoilTestState } from '../../shared/helpers/testing/recoilTestState';
import { LanguageName, LANGUAGE_CSHARP, LANGUAGE_FSHARP, LANGUAGE_IL, LANGUAGE_VB } from '../../shared/languages';
import { languageOptionState } from '../../shared/state/languageOptionState';
import type { FlowStep } from '../../shared/resultTypes';
import { StableCodeEditor } from './StableCodeEditor';

export default {
    component: StableCodeEditor
};

type TemplateProps = {
    language: LanguageName;
    initialCode: string;
    executionFlow?: ReadonlyArray<FlowStep>;
};
// eslint-disable-next-line @typescript-eslint/no-empty-function
const doNothing = () => {};

const Template: React.FC<TemplateProps> = ({ language, initialCode, executionFlow }) => <>
    <RecoilRoot initializeState={recoilTestState(
        [languageOptionState, language]
    )}>
        <StableCodeEditor
            initialCode={initialCode}
            onCodeChange={doNothing}
            onConnectionChange={doNothing}
            onServerError={doNothing}
            onSlowUpdateResult={doNothing}
            onSlowUpdateWait={doNothing}
            executionFlow={executionFlow ?? null}
            initialCached />
    </RecoilRoot>
</>;

const DarkMode = (Story: {
    (): JSX.Element;
    readonly storyName: string;
}) => {
    const story = () => <DarkModeRoot><Story /></DarkModeRoot>;
    story.storyName = Story.storyName + ' (Dark Mode)';
    return story;
};

export const CSharp = () => <Template language={LANGUAGE_CSHARP} initialCode={`
using System;
public class C {
    public void M() {
        var number = 1;
        var @string = "abc";
    }
}
`.trim()} />;
CSharp.storyName = 'C#';
export const CSharpDarkMode = DarkMode(CSharp);

export const VisualBasic = () => <Template language={LANGUAGE_VB} initialCode={`
Imports System
Public Class C
    Public Sub M() {
        Dim number = 1;
        Dim [string] = "abc";
    End Sub
End Class
`.trim()} />;
VisualBasic.storyName = 'Visual Basic';
export const VisualBasicDarkMode = DarkMode(VisualBasic);

export const FSharp = () => <Template language={LANGUAGE_FSHARP} initialCode={`
open System;

let number = 1;
let string = "abc";
`.trim()} />;
FSharp.storyName = 'F#';
export const FSharpDarkMode = DarkMode(FSharp);

export const IL = () => <Template language={LANGUAGE_IL} initialCode={`
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
IL.storyName = 'IL';
export const ILDarkMode = DarkMode(IL);

export const ExecutionFlow = () => <Template language={LANGUAGE_CSHARP} initialCode={`
Test(0);
Test(1);

static int Test(int x) {
    return x;
}
`.trim()} executionFlow={[
    { line: 1 },
    { line: 4, notes: 'x: 0' },
    { line: 6, notes: 'return: 0' },
    { line: 2 },
    { line: 4, notes: 'x: 1' },
    { line: 6, notes: 'return: 1' }
]} />;
ExecutionFlow.storyName = 'Execution Flow';
export const ExecutionFlowDarkMode = DarkMode(ExecutionFlow);

export const ExecutionFlowException = () => <Template language={LANGUAGE_CSHARP} initialCode={`
try {
    throw new();
}
catch {
}
`.trim()} executionFlow={[
    { line: 1 },
    { line: 2, exception: 'Exception' },
    { line: 4 },
    { line: 5 }
]} />;
ExecutionFlowException.storyName = 'Execution Flow Exception';
export const ExecutionFlowExceptionDarkMode = DarkMode(ExecutionFlowException);
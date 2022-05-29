import React from 'react';
import { RecoilRoot } from 'recoil';
import { DarkModeRoot } from '../../shared/testing/DarkModeRoot';
import { recoilTestState } from '../../shared/helpers/testing/recoilTestState';
import { LanguageName, LANGUAGE_CSHARP, LANGUAGE_FSHARP, LANGUAGE_IL, LANGUAGE_VB } from '../../shared/languages';
import { languageOptionState } from '../../shared/state/languageOptionState';
import { loadedCodeState } from '../../shared/state/loadedCodeState';
import { PreviewCodeEditor } from './PreviewCodeEditor';

export default {
    component: PreviewCodeEditor
};

type TemplateProps = {
    language: LanguageName;
    loadedCode: string;
};
// eslint-disable-next-line @typescript-eslint/no-empty-function
const doNothing = () => {};

const Template: React.FC<TemplateProps> = ({ language, loadedCode }) => <>
    <RecoilRoot initializeState={recoilTestState(
        [languageOptionState, language],
        [loadedCodeState, loadedCode]
    )}>
        <PreviewCodeEditor
            onCodeChange={doNothing}
            onConnectionChange={doNothing}
            onServerError={doNothing}
            onSlowUpdateResult={doNothing}
            onSlowUpdateWait={doNothing} />
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

export const VisualBasic = () => <Template language={LANGUAGE_VB} loadedCode={`
Imports System
Public Class C
    Public Sub M() {
        Dim number = 1;
        Dim [string] = "abc";
    End Sub
End Class
`.trim()} />;

export const FSharp = () => <Template language={LANGUAGE_FSHARP} loadedCode={`
open System;

let number = 1;
let string = "abc";
`.trim()} />;
FSharp.storyName = 'F#';

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

export const CSharpDarkMode = () => <DarkModeRoot><CSharp /></DarkModeRoot>;
CSharpDarkMode.storyName = 'C# (Dark Mode)';
export const VisualBasicDarkMode = () => <DarkModeRoot><VisualBasic /></DarkModeRoot>;
VisualBasicDarkMode.storyName = 'Visual Basic (Dark Mode)';
export const FSharpDarkMode = () => <DarkModeRoot><FSharp /></DarkModeRoot>;
FSharpDarkMode.storyName = 'F# (Dark Mode)';
export const ILDarkMode = () => <DarkModeRoot><IL /></DarkModeRoot>;
ILDarkMode.storyName = 'IL (Dark Mode)';
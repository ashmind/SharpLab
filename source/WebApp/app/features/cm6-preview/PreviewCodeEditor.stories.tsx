import React from 'react';
import { TestSetRecoilState } from '../../shared/helpers/testing/TestSetRecoilState';
import { TestWaitForRecoilStates } from '../../shared/helpers/testing/TestWaitForRecoilStates';
import { LanguageName, LANGUAGE_CSHARP, LANGUAGE_FSHARP, LANGUAGE_IL, LANGUAGE_VB } from '../../shared/languages';
import { languageOptionState } from '../../shared/state/languageOptionState';
import { loadedCodeState } from '../../shared/state/loadedCodeState';
import { darkModeStory } from '../../shared/testing/darkModeStory';
import { UserTheme, userThemeState } from '../dark-mode/themeState';
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
    <TestSetRecoilState state={languageOptionState} value={language} />
    <TestSetRecoilState state={loadedCodeState} value={loadedCode} />
    <TestSetRecoilState state={userThemeState} value={'light' as UserTheme} />
    <TestWaitForRecoilStates states={[languageOptionState, loadedCodeState, userThemeState]}>
        <PreviewCodeEditor
            onCodeChange={doNothing}
            onConnectionChange={doNothing}
            onServerError={doNothing}
            onSlowUpdateResult={doNothing}
            onSlowUpdateWait={doNothing}
            initialCached />
    </TestWaitForRecoilStates>
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

export const CSharpDarkMode = darkModeStory(CSharp);
export const VisualBasicDarkMode = darkModeStory(VisualBasic);
export const FSharpDarkMode = darkModeStory(FSharp);
export const ILDarkMode = darkModeStory(IL);
import React from 'react';
import { RecoilRoot } from 'recoil';
import { recoilTestState } from '../helpers/testing/recoilTestState';
import { getDefaultCode } from '../shared/defaults';
import { LanguageName, LANGUAGE_CSHARP } from '../shared/languages';
import { initialCodeState } from '../shared/state/initialCodeState';
import { languageOptionState } from '../shared/state/languageOptionState';
import { targetOptionState } from '../shared/state/targetOptionState';
import { TargetName, TARGET_CSHARP } from '../shared/targets';
import { DarkModeRoot } from '../shared/testing/DarkModeRoot';
import { ResultStateRoot } from '../shared/testing/ResultStateRoot';
import { Main } from './Main';

export default {
    component: Main
};

const EXAMPLE_RESULT_CODE = `
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Permissions;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.Default | DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints | DebuggableAttribute.DebuggingModes.EnableEditAndContinue | DebuggableAttribute.DebuggingModes.DisableOptimizations)]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[assembly: AssemblyVersion("0.0.0.0")]
[module: UnverifiableCode]
public class C
{
    public void M()
    {
    }
}
`.trim();

const Template = () => {
    return <>
        <style>{`
            #root {
                display: flex;
                height: 100%;
            }
        `}</style>
        <RecoilRoot initializeState={recoilTestState(
            [languageOptionState, LANGUAGE_CSHARP as LanguageName],
            [targetOptionState, TARGET_CSHARP as TargetName],
            [initialCodeState, getDefaultCode(LANGUAGE_CSHARP, TARGET_CSHARP)]
        )}>
            <ResultStateRoot action={{
                type: 'cachedResult',
                updateResult: {
                    diagnostics: [],
                    cached: { date: new Date() },
                    x: EXAMPLE_RESULT_CODE
                }
            }} waitForFirstResult>
                <Main />
            </ResultStateRoot>
        </RecoilRoot>
    </>;
};

export const Default = () => <Template />;
export const DefaultDarkMode = () => <DarkModeRoot><Default /></DarkModeRoot>;
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
import { EXAMPLE_CSHARP_CODE } from './results/CodeView.stories';

export default {
    component: Main
};

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
                    cached: { date: new Date('2000-01-01T00:00:00.000Z') },
                    x: EXAMPLE_CSHARP_CODE
                }
            }} waitForFirstResult>
                <Main />
            </ResultStateRoot>
        </RecoilRoot>
    </>;
};

export const Default = () => <Template />;
export const DefaultDarkMode = () => <DarkModeRoot><Default /></DarkModeRoot>;
import React from 'react';
import { RecoilRoot } from 'recoil';
import { recoilTestState } from '../shared/helpers/testing/recoilTestState';
import { LanguageName, LANGUAGE_CSHARP } from '../shared/languages';
import { languageOptionState } from '../shared/state/languageOptionState';
import { DarkModeRoot } from '../shared/testing/DarkModeRoot';
import { CodeSection } from './CodeSection';

export default {
    component: CodeSection
};

export const Default = () => <RecoilRoot initializeState={recoilTestState(
    [languageOptionState, LANGUAGE_CSHARP as LanguageName]
)}>
    <CodeSection codeEditor={<code>[Code Editor]</code>} />
</RecoilRoot>;
export const DefaultDarkMode = () => <DarkModeRoot><Default /></DarkModeRoot>;
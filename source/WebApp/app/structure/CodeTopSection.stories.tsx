import React from 'react';
import { RecoilRoot } from 'recoil';
import { recoilTestState } from '../helpers/testing/recoilTestState';
import { LanguageName, LANGUAGE_CSHARP } from '../shared/languages';
import { languageOptionState } from '../shared/state/languageOptionState';
import { DarkModeRoot } from '../shared/testing/DarkModeRoot';
import { CodeTopSection } from './CodeTopSection';

export default {
    component: CodeTopSection
};

export const Default = () => <RecoilRoot initializeState={recoilTestState(
    [languageOptionState, LANGUAGE_CSHARP as LanguageName]
)}>
    <CodeTopSection codeEditor={<code>[Code Editor]</code>} />
</RecoilRoot>;
export const DefaultDarkMode = () => <DarkModeRoot><Default /></DarkModeRoot>;
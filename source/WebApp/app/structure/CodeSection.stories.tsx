import React from 'react';
import { TestSetRecoilState } from '../shared/helpers/testing/TestSetRecoilState';
import { LanguageName, LANGUAGE_CSHARP } from '../shared/languages';
import { languageOptionState } from '../shared/state/languageOptionState';
import { darkModeStory } from '../shared/testing/darkModeStory';
import { CodeSection } from './CodeSection';

export default {
    component: CodeSection
};

export const Default = () => <TestSetRecoilState state={languageOptionState} value={LANGUAGE_CSHARP as LanguageName}>
    <CodeSection codeEditor={<code>[Code Editor]</code>} />
</TestSetRecoilState>;
export const DefaultDarkMode = darkModeStory(Default);
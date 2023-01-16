import React from 'react';
import { TestSetRecoilState } from './helpers/testing/TestSetRecoilState';
import { LanguageName, LANGUAGE_CSHARP } from './languages';
import { LanguageSelect } from './LanguageSelect';
import { languageOptionState } from './state/languageOptionState';
import { darkModeStory } from './testing/darkModeStory';

export default {
    component: LanguageSelect
};

type TemplateProps = {
    language: LanguageName;
};
const Template: React.FC<TemplateProps> = ({ language }) => {
    return <header>
        <TestSetRecoilState state={languageOptionState} value={language}>
            <LanguageSelect />
        </TestSetRecoilState>
    </header>;
};

export const Default = () => <Template language={LANGUAGE_CSHARP} />;
export const DarkMode = darkModeStory(Default);
import React from 'react';
import { RecoilRoot } from 'recoil';
import { recoilTestState } from '../shared/helpers/testing/recoilTestState';
import { LanguageName, LANGUAGE_CSHARP } from './languages';
import { LanguageSelect } from './LanguageSelect';
import { languageOptionState } from './state/languageOptionState';
import { DarkModeRoot } from './testing/DarkModeRoot';

export default {
    component: LanguageSelect
};

type TemplateProps = {
    language: LanguageName;
};
const Template: React.FC<TemplateProps> = ({ language }) => {
    return <header>
        <RecoilRoot initializeState={recoilTestState([languageOptionState, language])}>
            <LanguageSelect />
        </RecoilRoot>
    </header>;
};

export const Default = () => <Template language={LANGUAGE_CSHARP} />;
export const DarkMode = () => <DarkModeRoot><Default /></DarkModeRoot>;
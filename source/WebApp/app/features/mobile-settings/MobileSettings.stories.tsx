import React from 'react';
import { RecoilRoot } from 'recoil';
import { recoilTestState } from '../../shared/helpers/testing/recoilTestState';
import { languageOptionState } from '../../shared/state/languageOptionState';
import { LanguageName, LANGUAGE_CSHARP } from '../../shared/languages';
import { targetOptionState } from '../../shared/state/targetOptionState';
import { TargetName, TARGET_CSHARP } from '../../shared/targets';
import { DarkModeRoot } from '../../shared/testing/DarkModeRoot';
import { MOBILE_VIEWPORT } from '../../shared/helpers/testing/mobileViewport';
import { MobileSettings } from './MobileSettings';

export default {
    component: MobileSettings,
    parameters: {
        viewport: MOBILE_VIEWPORT
    }
};

type TemplateProps = {
    modalOpen?: boolean;
};
const minimalState = recoilTestState(
    [languageOptionState, LANGUAGE_CSHARP as LanguageName],
    [targetOptionState, TARGET_CSHARP as TargetName]
);
const Template: React.FC<TemplateProps> = ({ modalOpen } = {}) => <RecoilRoot initializeState={minimalState}>
    <MobileSettings buttonProps={{}} initialState={{ modalOpen }} />
</RecoilRoot>;

export const Default = () => <Template />;
export const DarkMode = () => <DarkModeRoot><Template /></DarkModeRoot>;
export const ModalOpen = () => <Template modalOpen />;
export const ModalOpenDarkMode = () => <DarkModeRoot><Template modalOpen /></DarkModeRoot>;
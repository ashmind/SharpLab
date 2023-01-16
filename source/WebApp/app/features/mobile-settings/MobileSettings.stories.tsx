import React from 'react';
import { languageOptionState } from '../../shared/state/languageOptionState';
import { LanguageName, LANGUAGE_CSHARP } from '../../shared/languages';
import { targetOptionState } from '../../shared/state/targetOptionState';
import { TargetName, TARGET_CSHARP } from '../../shared/targets';
import { MOBILE_VIEWPORT } from '../../shared/helpers/testing/mobileViewport';
import { TestSetRecoilState } from '../../shared/helpers/testing/TestSetRecoilState';
import { TestWaitForRecoilStates } from '../../shared/helpers/testing/TestWaitForRecoilStates';
import { darkModeStory } from '../../shared/testing/darkModeStory';
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
const Template: React.FC<TemplateProps> = ({ modalOpen } = {}) => {
    return <>
        <TestSetRecoilState state={languageOptionState} value={LANGUAGE_CSHARP as LanguageName} />
        <TestSetRecoilState state={targetOptionState} value={TARGET_CSHARP as TargetName} />
        <TestWaitForRecoilStates states={[languageOptionState, targetOptionState]}>
            <MobileSettings buttonProps={{}} initialState={{ modalOpen }} />
        </TestWaitForRecoilStates>
    </>;
};

export const Default = () => <Template />;
export const DarkMode = darkModeStory(Default);
export const ModalOpen = () => <Template modalOpen />;
export const ModalOpenDarkMode = darkModeStory(ModalOpen);
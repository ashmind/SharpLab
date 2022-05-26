import React from 'react';
import { RecoilRoot } from 'recoil';
import { recoilTestState } from '../../helpers/testing/recoilTestState';
import { DarkModeRoot } from '../../shared/testing/DarkModeRoot';
import { MOBILE_VIEWPORT } from '../../helpers/testing/mobileViewport';
import { fontSizeState, MobileFontSize } from './fontSizeState';
import { MobileFontSizeSwitch } from './MobileFontSizeSwitch';

export default {
    component: MobileFontSizeSwitch,
    parameters: {
        viewport: MOBILE_VIEWPORT
    }
};

type TemplateProps = {
    fontSize: MobileFontSize;
};
const Template: React.FC<TemplateProps> = ({ fontSize }) => <>
    <main>
        <pre className="CodeMirror" style={{ position: 'relative' }}>
            Example code size
        </pre>
    </main>
    <footer>
        <RecoilRoot initializeState={recoilTestState([fontSizeState, fontSize])}>
            <MobileFontSizeSwitch />
        </RecoilRoot>
    </footer>
</>;

export const Default = () => <Template fontSize='default' />;
export const Large = () => <Template fontSize='large' />;
export const DarkMode = () => <DarkModeRoot><Template fontSize='default' /></DarkModeRoot>;
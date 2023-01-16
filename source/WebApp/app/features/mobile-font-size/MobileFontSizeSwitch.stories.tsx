import React from 'react';
import { MOBILE_VIEWPORT } from '../../shared/helpers/testing/mobileViewport';
import { TestSetRecoilState } from '../../shared/helpers/testing/TestSetRecoilState';
import { darkModeStory } from '../../shared/testing/darkModeStory';
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
        <TestSetRecoilState state={fontSizeState} value={fontSize}>
            <MobileFontSizeSwitch />
        </TestSetRecoilState>
    </footer>
</>;

export const Default = () => <Template fontSize='default' />;
export const Large = () => <Template fontSize='large' />;
export const DarkMode = darkModeStory(Default);
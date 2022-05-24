import React from 'react';
import { RecoilRoot } from 'recoil';
import { INITIAL_VIEWPORTS } from '@storybook/addon-viewport';
import { recoilTestState } from '../../helpers/testing/recoilTestState';
import { fontSizeState, MobileFontSize } from './fontSizeState';
import { MobileFontSizeSwitch } from './MobileFontSizeSwitch';

export default {
    component: MobileFontSizeSwitch,
    parameters: {
        viewport: {
            viewports: INITIAL_VIEWPORTS,
            defaultViewport: 'pixel'
        }
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
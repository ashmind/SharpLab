import React from 'react';
import { TestSetRecoilState } from './helpers/testing/TestSetRecoilState';
import { ModeSelect } from './ModeSelect';
import { releaseOptionState } from './state/releaseOptionState';
import { darkModeStory } from './testing/darkModeStory';

export default {
    component: ModeSelect
};

type TemplateProps = {
    release?: boolean;
};
const Template: React.FC<TemplateProps> = ({ release }) => {
    return <header>
        <TestSetRecoilState state={releaseOptionState} value={release ?? false}>
            <ModeSelect />
        </TestSetRecoilState>
    </header>;
};

export const Debug = () => <Template />;
export const Release = () => <Template release />;
export const DarkMode = darkModeStory(Release);
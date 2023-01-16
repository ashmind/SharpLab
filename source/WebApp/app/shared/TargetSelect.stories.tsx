import React from 'react';
import { TestSetRecoilState } from './helpers/testing/TestSetRecoilState';
import { targetOptionState } from './state/targetOptionState';
import { type TargetName, TARGET_CSHARP, TARGET_RUN } from './targets';
import { TargetSelect } from './TargetSelect';
import { darkModeStory } from './testing/darkModeStory';

export default {
    component: TargetSelect
};

type TemplateProps = {
    target: TargetName;
};
const Template: React.FC<TemplateProps> = ({ target }) => {
    return <header>
        <TestSetRecoilState state={targetOptionState} value={target}>
            <TargetSelect />
        </TestSetRecoilState>
    </header>;
};

export const Default = () => <Template target={TARGET_CSHARP} />;
export const Run = () => <Template target={TARGET_RUN} />;
export const DarkMode = darkModeStory(Default);
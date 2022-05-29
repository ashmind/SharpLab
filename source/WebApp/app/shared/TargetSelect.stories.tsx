import React from 'react';
import { RecoilRoot } from 'recoil';
import { recoilTestState } from '../shared/helpers/testing/recoilTestState';
import { targetOptionState } from './state/targetOptionState';
import { type TargetName, TARGET_CSHARP, TARGET_RUN } from './targets';
import { TargetSelect } from './TargetSelect';
import { DarkModeRoot } from './testing/DarkModeRoot';

export default {
    component: TargetSelect
};

type TemplateProps = {
    target: TargetName;
};
const Template: React.FC<TemplateProps> = ({ target }) => {
    return <header>
        <RecoilRoot initializeState={recoilTestState([targetOptionState, target])}>
            <TargetSelect />
        </RecoilRoot>
    </header>;
};

export const Default = () => <Template target={TARGET_CSHARP} />;
export const Run = () => <Template target={TARGET_RUN} />;
export const DarkMode = () => <DarkModeRoot><Default /></DarkModeRoot>;
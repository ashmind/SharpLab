import React from 'react';
import { RecoilRoot } from 'recoil';
import { recoilTestState } from '../shared/helpers/testing/recoilTestState';
import { ModeSelect } from './ModeSelect';
import { releaseOptionState } from './state/releaseOptionState';
import { DarkModeRoot } from './testing/DarkModeRoot';

export default {
    component: ModeSelect
};

type TemplateProps = {
    release?: boolean;
};
const Template: React.FC<TemplateProps> = ({ release }) => {
    return <header>
        <RecoilRoot initializeState={recoilTestState([releaseOptionState, release ?? false])}>
            <ModeSelect />
        </RecoilRoot>
    </header>;
};

export const Debug = () => <Template />;
export const Release = () => <Template release />;
export const DarkMode = () => <DarkModeRoot><Release /></DarkModeRoot>;
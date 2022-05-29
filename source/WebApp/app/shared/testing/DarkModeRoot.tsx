import React from 'react';
import { RecoilRoot } from 'recoil';
import { DarkModeSwitch } from '../../features/dark-mode/DarkModeSwitch';
import { UserTheme, userThemeState } from '../../features/dark-mode/themeState';
import { recoilTestState } from '../../shared/helpers/testing/recoilTestState';

type Props = {
    children: React.ReactNode;
};

export const DarkModeRoot: React.FC<Props> = ({ children }) => <>
    {children}
    <div style={{ display: 'none' }}>
        <RecoilRoot initializeState={recoilTestState([userThemeState, 'dark' as UserTheme])}>
            <DarkModeSwitch />
        </RecoilRoot>
    </div>
</>;
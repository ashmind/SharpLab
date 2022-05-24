import React from 'react';
import { RecoilRoot } from 'recoil';
import { recoilTestState } from '../../helpers/testing/recoilTestState';
import { DarkModeSwitch } from './DarkModeSwitch';
import { UserTheme, userThemeState } from './themeState';

export default {
    component: DarkModeSwitch
};

type TemplateProps = {
    userTheme: UserTheme;
};
const Template: React.FC<TemplateProps> = ({ userTheme }) => <>
    <main />{/* needed for some styles to apply */}
    <footer>
        <RecoilRoot initializeState={recoilTestState([userThemeState, userTheme])}>
            <DarkModeSwitch />
        </RecoilRoot>
    </footer>
</>;

export const Auto = () => <Template userTheme='auto' />;
export const Light = () => <Template userTheme='light' />;
export const Dark = () => <Template userTheme='dark' />;
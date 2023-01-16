import React from 'react';
import { TestSetRecoilState } from '../../shared/helpers/testing/TestSetRecoilState';
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
        <TestSetRecoilState state={userThemeState} value={userTheme}>
            <DarkModeSwitch />
        </TestSetRecoilState>
    </footer>
</>;

export const Auto = () => <Template userTheme='auto' />;
export const Light = () => <Template userTheme='light' />;
export const Dark = () => <Template userTheme='dark' />;
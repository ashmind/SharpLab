import React from 'react';
import { DarkModeSwitch } from '../../features/dark-mode/DarkModeSwitch';
import { UserTheme, userThemeState } from '../../features/dark-mode/themeState';
import { TestSetRecoilState } from '../helpers/testing/TestSetRecoilState';

export const darkModeStory = (Story: {
    (): JSX.Element;
    storyName?: string;
}) => {
    const result = () => <>
        <Story />
        <TestSetRecoilState state={userThemeState} value={'dark' as UserTheme} />
        <div style={{ display: 'none' }}>
            <DarkModeSwitch />
        </div>
    </>;
    if (Story.storyName)
        result.storyName = Story.storyName + ' Dark Mode';
    return result;
};
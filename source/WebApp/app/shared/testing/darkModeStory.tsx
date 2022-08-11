import React from 'react';
import { DarkModeRoot } from './DarkModeRoot';

export const darkModeStory = (Story: {
    (): JSX.Element;
    storyName?: string;
}) => {
    const result = () => <DarkModeRoot><Story /></DarkModeRoot>;
    if (Story.storyName)
        result.storyName = Story.storyName + ' Dark Mode';
    return result;
};
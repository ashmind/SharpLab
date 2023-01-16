import React from 'react';
import { darkModeStory } from '../../shared/testing/darkModeStory';
import { GistSaveModal } from './GistSaveModal';

export default {
    component: GistSaveModal
};

// eslint-disable-next-line @typescript-eslint/ban-types
type TemplateProps = {};

// eslint-disable-next-line @typescript-eslint/no-empty-function
const doNothing = () => {};
const Template: React.FC<TemplateProps> = () => {
    return <GistSaveModal onSave={doNothing} onCancel={doNothing} />;
};

export const Default = () => <Template />;
export const DarkMode = darkModeStory(Default);
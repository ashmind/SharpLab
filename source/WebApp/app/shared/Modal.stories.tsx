import React from 'react';
import { Modal } from './Modal';
import { darkModeStory } from './testing/darkModeStory';

export default {
    component: Modal
};

export const Default = () => <Modal title="Test Title">Test Content</Modal>;
export const DarkMode = darkModeStory(Default);
import React from 'react';
import { darkModeStory } from '../../shared/testing/darkModeStory';
import { VerifyView } from './VerifyView';

export default {
    component: VerifyView
};

export const Success = () => <VerifyView message='✔️ Compilation completed.' />;
export const SuccessDarkMode = darkModeStory(Success);
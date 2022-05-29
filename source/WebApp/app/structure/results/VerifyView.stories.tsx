import React from 'react';
import { DarkModeRoot } from '../../shared/testing/DarkModeRoot';
import { VerifyView } from './VerifyView';

export default {
    component: VerifyView
};

export const Success = () => <VerifyView message='✔️ Compilation completed.' />;
export const SuccessDarkMode = () => <DarkModeRoot><Success /></DarkModeRoot>;
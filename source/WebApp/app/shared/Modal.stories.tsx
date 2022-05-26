import React from 'react';
import { Modal } from './Modal';
import { DarkModeRoot } from './testing/DarkModeRoot';

export default {
    component: Modal
};

export const Default = () => <Modal title="Test Title">Test Content</Modal>;
export const DarkMode = () => <DarkModeRoot><Default /></DarkModeRoot>;
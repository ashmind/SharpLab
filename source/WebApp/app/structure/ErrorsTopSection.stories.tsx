import React from 'react';
import { DarkModeRoot } from '../shared/testing/DarkModeRoot';
import { ErrorsTopSection } from './ErrorsTopSection';

export default {
    component: ErrorsTopSection
};

export const Empty = () => <ErrorsTopSection errors={[]} />;
export const Full = () => <ErrorsTopSection errors={[
    { id: 'CS1525', message: "Invalid expression term ';'", severity: 'error' },
    { id: 'CS1514', message: '{ expected', severity: 'error' },
    { id: 'CS1513', message: '} expected', severity: 'error' }
]} />;
export const FullDarkMode = () => <DarkModeRoot><Full /></DarkModeRoot>;
import React from 'react';
import { DarkModeRoot } from '../shared/testing/DarkModeRoot';
import { ErrorsTopSection } from './ErrorsTopSection';

export default {
    component: ErrorsTopSection,
    excludeStories: /^EXAMPLE_/
};

export const EXAMPLE_ERRORS = [
    { id: 'CS1525', message: "Invalid expression term ';'", severity: 'error' },
    { id: 'CS1514', message: '{ expected', severity: 'error' },
    { id: 'CS1513', message: '} expected', severity: 'error' }
] as const;

export const Empty = () => <ErrorsTopSection errors={[]} />;
export const Full = () => <ErrorsTopSection errors={EXAMPLE_ERRORS} />;
export const FullDarkMode = () => <DarkModeRoot><Full /></DarkModeRoot>;
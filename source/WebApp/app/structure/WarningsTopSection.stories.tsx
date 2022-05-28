import React from 'react';
import { DarkModeRoot } from '../shared/testing/DarkModeRoot';
import { WarningsTopSection } from './WarningsTopSection';

export default {
    component: WarningsTopSection,
    excludeStories: /^EXAMPLE_/
};

export const EXAMPLE_WARNINGS = [
    { id: 'CS0219', message: "The variable 'x' is assigned but its value is never used", severity: 'warning' },
    { id: 'CS0105', message: "The using directive for 'System' appeared previously in this namespace", severity: 'warning' }
] as const;

export const Empty = () => <WarningsTopSection warnings={[]} />;

export const Expanded = () => <WarningsTopSection warnings={EXAMPLE_WARNINGS} initialState={{ expanded: true }} />;
export const ExpandedDarkMode = () => <DarkModeRoot><Expanded /></DarkModeRoot>;

export const Collapsed = () => <WarningsTopSection warnings={EXAMPLE_WARNINGS} />;
export const CollapsedDarkMode = () => <DarkModeRoot><Collapsed /></DarkModeRoot>;
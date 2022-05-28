import React from 'react';
import { DarkModeRoot } from '../shared/testing/DarkModeRoot';
import { WarningsSection } from './WarningsSection';

export default {
    component: WarningsSection,
    excludeStories: /^EXAMPLE_/
};

export const EXAMPLE_WARNINGS = [
    { id: 'CS0219', message: "The variable 'x' is assigned but its value is never used", severity: 'warning' },
    { id: 'CS0105', message: "The using directive for 'System' appeared previously in this namespace", severity: 'warning' }
] as const;

export const Empty = () => <WarningsSection warnings={[]} />;

export const Expanded = () => <WarningsSection warnings={EXAMPLE_WARNINGS} initialState={{ expanded: true }} />;
export const ExpandedDarkMode = () => <DarkModeRoot><Expanded /></DarkModeRoot>;

export const Collapsed = () => <WarningsSection warnings={EXAMPLE_WARNINGS} />;
export const CollapsedDarkMode = () => <DarkModeRoot><Collapsed /></DarkModeRoot>;
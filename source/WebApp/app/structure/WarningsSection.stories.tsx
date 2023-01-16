import React from 'react';
import { darkModeStory } from '../shared/testing/darkModeStory';
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
export const ExpandedDarkMode = darkModeStory(Expanded);

export const Collapsed = () => <WarningsSection warnings={EXAMPLE_WARNINGS} />;
export const CollapsedDarkMode = darkModeStory(Collapsed);
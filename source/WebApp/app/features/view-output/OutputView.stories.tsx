import React from 'react';
import { LANGUAGE_CSHARP } from '../../shared/languages';
import type { OutputItem } from '../../shared/resultTypes';
import { darkModeStory } from '../../shared/testing/darkModeStory';
import type { InspectionGroup } from './internal/GroupOutput';
import { OutputView } from './OutputView';

export default {
    component: OutputView,
    excludeStories: /^EXAMPLE_/
};

export const EXAMPLE_OUTPUT = [
    'Console 1\r\n',
    {
        type: 'inspection:simple',
        title: 'Inspect',
        value: 'Inspect'
    },
    'Console 2, \n  Multiline\r\n',
    {
        type: 'inspection:simple',
        title: 'Exception',
        value: 'System.Exception: Exception\r\n   at <Program>$.<Main>$(String[] args)'
    }
] as const;

type TemplateProps = {
    output: ReadonlyArray<OutputItem|InspectionGroup>;
};
const Template: React.FC<TemplateProps> = ({ output }) => {
    return <OutputView output={output} sourceCode={''} sourceLanguage={LANGUAGE_CSHARP} />;
};

export const Empty = () => <Template output={[]} />;
export const EmptyDarkMode = darkModeStory(Empty);

export const Full = () => <Template output={EXAMPLE_OUTPUT} />;
export const FullDarkMode = darkModeStory(Full);
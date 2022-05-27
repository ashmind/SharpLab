import React from 'react';
import { DarkModeRoot } from '../../shared/testing/DarkModeRoot';
import { ExplainView } from './ExplainView';

export default {
    component: ExplainView,
    excludeStories: /^EXAMPLE_/
};

export const EXAMPLE_EXPLANATIONS = [
    {
        code: '@x ',
        name: 'verbatim identifier',
        text: "Prefix `@` allows keywords to be used as a name (for a variable, parameter, field, class, etc).  \nFor example, `M(string string)` doesn't compile, while `M(string @string)` does.\n",
        link: 'https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/tokens/verbatim'
    },
    {
        code: '(1, 1)',
        name: 'tuple',
        text: 'Tuples are types that group several values together. Values can be named, e.g. `(a: 1, b: 2)` or unnamed, e.g. `(1, 2)`.',
        link: 'https://docs.microsoft.com/en-us/dotnet/csharp/tuples'
    }
];

export const Empty = () => <ExplainView explanations={[]} />;
export const EmptyDarkMode = () => <DarkModeRoot><Empty /></DarkModeRoot>;

export const Full = () => <ExplainView explanations={EXAMPLE_EXPLANATIONS} />;
export const FullDarkMode = () => <DarkModeRoot><Full /></DarkModeRoot>;
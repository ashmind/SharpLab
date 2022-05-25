import React from 'react';
import { RecoilRoot } from 'recoil';
import { recoilTestState } from '../../helpers/testing/recoilTestState';
import { DarkModeRoot } from '../../helpers/testing/DarkModeRoot';
import { fromPartial } from '../../helpers/testing/fromPartial';
import { BranchDetailsSection } from './BranchDetailsSection';
import { branchOptionState } from './branchOptionState';
import type { Branch } from './types';

export default {
    component: BranchDetailsSection,
    excludeStories: ['EXAMPLE_BRANCH']
};

type TemplateProps = {
    branch?: Branch;
    headerless?: boolean;
    expanded?: boolean;
};
const Template: React.FC<TemplateProps> = ({ branch, headerless, expanded } = {}) => <RecoilRoot initializeState={recoilTestState(
    [branchOptionState, branch ?? null]
)}>
    <BranchDetailsSection headerless={headerless} initialState={{ expanded }} />
</RecoilRoot>;

export const EXAMPLE_BRANCH = fromPartial<Branch>({
    displayName: 'main (11 May 2022)',
    commits: [{
        hash: 'e6b5dd830f1b790bd80e62272129cc040d0a2fdc',
        author: 'Jane Roslyn',
        message: `
            Fix all issues with the flux capacitor.
            Add support for parsing Swift.
        `
    }]
});

export const Default = () => <Template branch={EXAMPLE_BRANCH} expanded />;
export const Headerless = () => <Template branch={EXAMPLE_BRANCH} headerless />;
export const DarkMode = () => <DarkModeRoot><Default /></DarkModeRoot>;
export const Collapsed = () => <Template branch={EXAMPLE_BRANCH} />;
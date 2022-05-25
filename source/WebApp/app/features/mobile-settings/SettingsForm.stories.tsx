import React from 'react';
import { RecoilRoot } from 'recoil';
import { recoilTestState } from '../../helpers/testing/recoilTestState';
import { languageOptionState } from '../../shared/state/languageOptionState';
import { LanguageName, LANGUAGE_CSHARP } from '../../shared/languages';
import { targetOptionState } from '../../shared/state/targetOptionState';
import { TargetName, TARGET_CSHARP } from '../../shared/targets';
import type { Gist } from '../save-as-gist/gist';
import { gistState } from '../save-as-gist/gistState';
import { fromPartial } from '../../helpers/testing/fromPartial';
import { codeState } from '../../shared/state/codeState';
import type { Branch } from '../roslyn-branches/types';
import { branchOptionState } from '../roslyn-branches/branchOptionState';
// eslint-disable-next-line import/extensions
import { EXAMPLE_BRANCH } from '../roslyn-branches/BranchDetailsSection.stories';
import { SettingsForm } from './SettingsForm';

export default {
    component: SettingsForm
};

// eslint-disable-next-line @typescript-eslint/ban-types
type TemplateProps = {
    branch?: Branch;
    gist?: Gist;
};
const Template: React.FC<TemplateProps> = ({ branch, gist } = {}) => {
    const state = recoilTestState(
        [languageOptionState, LANGUAGE_CSHARP as LanguageName],
        [targetOptionState, TARGET_CSHARP as TargetName],
        [branchOptionState, branch ?? null],
        [codeState, gist?.code ?? ''],
        [gistState, gist ?? null]
    );

    return <RecoilRoot initializeState={state}>
        <SettingsForm />
    </RecoilRoot>;
};

export const Default = () => <Template />;
export const WithBranchDetails = () => <Template branch={EXAMPLE_BRANCH} />;
export const WithGist = () => <Template gist={fromPartial({
    name: 'Test Gist',
    code: '_'
})} />;
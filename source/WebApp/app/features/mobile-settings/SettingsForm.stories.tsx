import React from 'react';
import { languageOptionState } from '../../shared/state/languageOptionState';
import { LanguageName, LANGUAGE_CSHARP } from '../../shared/languages';
import { targetOptionState } from '../../shared/state/targetOptionState';
import { TargetName, TARGET_CSHARP } from '../../shared/targets';
import type { Gist } from '../save-as-gist/Gist';
import { gistState } from '../save-as-gist/gistState';
import { fromPartial } from '../../shared/helpers/testing/fromPartial';
import { codeState } from '../../shared/state/codeState';
import type { Branch } from '../roslyn-branches/types';
import { branchOptionState } from '../roslyn-branches/branchOptionState';
// eslint-disable-next-line import/extensions
import { EXAMPLE_BRANCH } from '../roslyn-branches/BranchDetailsSection.stories';
import { TestSetRecoilState } from '../../shared/helpers/testing/TestSetRecoilState';
import { TestWaitForRecoilStates } from '../../shared/helpers/testing/TestWaitForRecoilStates';
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
    return <>
        <TestSetRecoilState state={languageOptionState} value={LANGUAGE_CSHARP as LanguageName} />
        <TestSetRecoilState state={targetOptionState} value={TARGET_CSHARP as TargetName} />
        <TestSetRecoilState state={branchOptionState} value={branch ?? null} />
        <TestSetRecoilState state={codeState} value={gist?.code ?? ''} />
        <TestSetRecoilState state={gistState} value={gist ?? null} />
        <TestWaitForRecoilStates states={[languageOptionState, targetOptionState, branchOptionState, codeState, gistState]}>
            <SettingsForm />
        </TestWaitForRecoilStates>
    </>;
};

export const Default = () => <Template />;
export const WithBranchDetails = () => <Template branch={EXAMPLE_BRANCH} />;
export const WithGist = () => <Template gist={fromPartial({
    name: 'Test Gist',
    code: '_',
    options: {}
})} />;
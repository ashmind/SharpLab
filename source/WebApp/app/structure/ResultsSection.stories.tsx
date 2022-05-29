import React from 'react';
import { RecoilRoot } from 'recoil';
import { recoilTestState } from '../shared/helpers/testing/recoilTestState';
import type { Result } from '../shared/resultTypes';
import { targetOptionState } from '../shared/state/targetOptionState';
import { TargetName, TARGET_AST, TARGET_CSHARP, TARGET_EXPLAIN, TARGET_RUN, TARGET_VERIFY } from '../shared/targets';
import { DarkModeRoot } from '../shared/testing/DarkModeRoot';
import { ResultRoot } from '../shared/testing/ResultRoot';
import { EXAMPLE_AST } from '../features/view-ast/AstView.stories';
import { EXAMPLE_CSHARP_CODE } from './results/CodeView.stories';
import { EXAMPLE_EXPLANATIONS } from './results/ExplainView.stories';
import { EXAMPLE_OUTPUT } from './results/OutputView.stories';
import { ResultsSection } from './ResultsSection';

export default {
    component: ResultsSection
};

type TemplateProps = {
    target: TargetName;
    value: Result['value'];
};

const Template: React.FC<TemplateProps> = ({ target, value }) => {
    return <>
        <style>{`
            #root {
                display: flex;
                height: 100%;
            }
        `}</style>
        <RecoilRoot initializeState={recoilTestState([targetOptionState, target])}>
            <ResultRoot action={{
                type: 'cachedResult',
                target,
                updateResult: {
                    diagnostics: [],
                    cached: { date: new Date('2000-01-01T00:00:00.000Z') },
                    x: value
                }
            }}>
                <ResultsSection />
            </ResultRoot>
        </RecoilRoot>
    </>;
};

export const Code = () => <Template target={TARGET_CSHARP} value={EXAMPLE_CSHARP_CODE} />;
export const CodeDarkMode = () => <DarkModeRoot><Code /></DarkModeRoot>;

export const Ast = () => <Template target={TARGET_AST} value={EXAMPLE_AST} />;
export const AstDarkMode = () => <DarkModeRoot><Ast /></DarkModeRoot>;

export const Explain = () => <Template target={TARGET_EXPLAIN} value={EXAMPLE_EXPLANATIONS} />;
export const ExplainDarkMode = () => <DarkModeRoot><Explain /></DarkModeRoot>;

export const Run = () => <Template target={TARGET_RUN} value={{ output: EXAMPLE_OUTPUT, flow: [] }} />;
export const RunDarkMode = () => <DarkModeRoot><Run /></DarkModeRoot>;

export const Verify = () => <Template target={TARGET_VERIFY} value='✔️ Compilation completed.' />;
export const VerifyDarkMode = () => <DarkModeRoot><Verify /></DarkModeRoot>;
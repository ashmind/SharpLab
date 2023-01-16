import React from 'react';
import type { Result } from '../shared/resultTypes';
import { targetOptionState } from '../shared/state/targetOptionState';
import { TargetName, TARGET_AST, TARGET_CSHARP, TARGET_EXPLAIN, TARGET_RUN, TARGET_VERIFY } from '../shared/targets';
import { ResultRoot } from '../shared/testing/ResultRoot';
import { EXAMPLE_AST } from '../features/view-ast/AstView.stories';
import { EXAMPLE_OUTPUT } from '../features/view-output/OutputView.stories';
import { TestSetRecoilState } from '../shared/helpers/testing/TestSetRecoilState';
import { darkModeStory } from '../shared/testing/darkModeStory';
import { EXAMPLE_CSHARP_CODE } from './results/CodeView.stories';
import { EXAMPLE_EXPLANATIONS } from './results/ExplainView.stories';
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
        <TestSetRecoilState state={targetOptionState} value={target}>
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
        </TestSetRecoilState>
    </>;
};

export const Code = () => <Template target={TARGET_CSHARP} value={EXAMPLE_CSHARP_CODE} />;
export const CodeDarkMode = darkModeStory(Code);

export const Ast = () => <Template target={TARGET_AST} value={EXAMPLE_AST} />;
export const AstDarkMode = darkModeStory(Ast);

export const Explain = () => <Template target={TARGET_EXPLAIN} value={EXAMPLE_EXPLANATIONS} />;
export const ExplainDarkMode = darkModeStory(Explain);

export const Run = () => <Template target={TARGET_RUN} value={{ output: EXAMPLE_OUTPUT, flow: [] }} />;
export const RunDarkMode = darkModeStory(Run);

export const Verify = () => <Template target={TARGET_VERIFY} value='✔️ Compilation completed.' />;
export const VerifyDarkMode = darkModeStory(Verify);
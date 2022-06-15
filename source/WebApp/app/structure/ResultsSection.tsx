import React, { useEffect, useState } from 'react';
import dateFormat from 'dateformat';
import { useRecoilCallback, useRecoilValue } from 'recoil';
import { Loader } from '../shared/Loader';
import { ModeSelect } from '../shared/ModeSelect';
import type { CodeResult, OutputItem, ParsedResult } from '../shared/resultTypes';
import { resultSelector } from '../shared/state/resultState';
import { targetOptionState } from '../shared/state/targetOptionState';
import type { TargetLanguageName } from '../shared/targets';
import { TargetSelect } from '../shared/TargetSelect';
import { AstView } from '../features/view-ast/AstView';
import { OutputView } from '../features/view-output/OutputView';
import type { LanguageName } from '../shared/languages';
import { languageOptionState } from '../shared/state/languageOptionState';
import { codeState } from '../shared/state/codeState';
import type { MaybeCached } from '../features/result-cache/types';
import { ExplainView } from './results/ExplainView';
import { VerifyView } from './results/VerifyView';
import { CodeView } from './results/CodeView';

type CodeState = Pick<CodeResult, 'value'|'ranges'> & { language: TargetLanguageName };
type ResultState = {
    sourceCode: string;
    sourceLanguage: LanguageName;
    result: MaybeCached<ParsedResult>;
};

const EMPTY_OUTPUT = [] as ReadonlyArray<OutputItem>;
export const ResultsSection: React.FC = () => {
    const [lastCodeState, setLastCodeState] = useState<CodeState>();
    const result = useRecoilValue(resultSelector);
    const [resultState, setResultState] = useState<ResultState | null>(null);

    const updateOnResultChange = useRecoilCallback(({ snapshot }) => (result: MaybeCached<ParsedResult> | undefined) => {
        if (result?.type === 'code') {
            const target = snapshot.getLoadable(targetOptionState).getValue();
            setLastCodeState({ ...result, language: target as TargetLanguageName });
        }

        setResultState(result ? {
            sourceCode: snapshot.getLoadable(codeState).getValue(),
            sourceLanguage: snapshot.getLoadable(languageOptionState).getValue(),
            result
        } : null);
    });

    // Code is special since CodeMirror is slow to set up, so we hide it instead of destroying it
    useEffect(() => updateOnResultChange(result), [updateOnResultChange, result]);
    const codeResult = lastCodeState && <div hidden={result?.type !== 'code'}>
        <CodeView
            code={lastCodeState.value ?? ''}
            ranges={lastCodeState.ranges}
            language={lastCodeState.language} />
    </div>;

    const renderNonCodeResult = () => {
        if (!resultState)
            return null;

        const { sourceCode, sourceLanguage, result } = resultState;
        switch (result.type) {
            case 'code':
                return null;
            case 'ast':
                return <AstView roots={result.value} />;
            case 'verify':
                return <VerifyView message={result.value ?? ''} />;
            case 'explain':
                return <ExplainView explanations={result.value} />;
            case 'run':
                return <OutputView
                    output={result.value?.output ?? EMPTY_OUTPUT}
                    sourceCode={sourceCode}
                    sourceLanguage={sourceLanguage}
                    flow={result.value?.flow}
                />;
        }
    };

    return <section className="top-section result">
        <header>
            <h1>Results</h1>
            <TargetSelect tabIndex={4} useAriaLabel />
            {result?.cached && <small className="result-cached-indicator" title="Note: This output is cached and might not represent the latest behavior">
                Cached: {dateFormat(result.cached.date, 'd mmm yyyy')}
            </small>}
            <ModeSelect tabIndex={5} useAriaLabel />
        </header>
        <div className="content">
            <Loader />
            {codeResult}
            {renderNonCodeResult()}
        </div>
    </section>;
};
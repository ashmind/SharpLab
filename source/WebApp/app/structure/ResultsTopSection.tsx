import React, { useEffect, useState } from 'react';
import dateFormat from 'dateformat';
import { useRecoilValue } from 'recoil';
import { Loader } from '../shared/Loader';
import { ModeSelect } from '../shared/ModeSelect';
import type { CodeResult, OutputItem } from '../shared/resultTypes';
import { resultSelector } from '../shared/state/resultState';
import { targetOptionState } from '../shared/state/targetOptionState';
import type { TargetLanguageName } from '../shared/targets';
import { TargetSelect } from '../shared/TargetSelect';
import { OutputView } from './results/OutputView';
import { ExplainView } from './results/ExplainView';
import { VerifyView } from './results/VerifyView';
import { AstView } from './results/AstView';
import { CodeView } from './results/CodeView';

type CodeState = Pick<CodeResult, 'value'|'ranges'> & { language: TargetLanguageName };

const EMPTY_OUTPUT = [] as ReadonlyArray<OutputItem>;
export const ResultsTopSection: React.FC = () => {
    const [lastCodeState, setLastCodeState] = useState<CodeState>();
    const target = useRecoilValue(targetOptionState);
    const result = useRecoilValue(resultSelector);

    // Code is special since CodeMirror is slow to set up, so we hide it instead of destroying it
    useEffect(() => {
        if (result?.type === 'code')
            setLastCodeState({ ...result, language: target as TargetLanguageName });
    // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [result]);
    const codeResult = lastCodeState && <div hidden={result?.type !== 'code'}>
        <CodeView
            code={lastCodeState.value ?? ''}
            ranges={lastCodeState.ranges}
            language={lastCodeState.language} />
    </div>;

    const renderNonCodeResult = () => {
        if (!result)
            return null;

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
                return <OutputView output={result.value?.output ?? EMPTY_OUTPUT} />;
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
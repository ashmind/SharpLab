import React, { FC, useEffect, useState } from 'react';
import dateFormat from 'dateformat';
import type { AppOptions } from 'ts/types/app';
import type { AstItem, Cacheable, CodeResult, OutputItem, Result, RunResult } from 'ts/types/results';
import type { TargetLanguageName } from 'ts/helpers/targets';
import { TargetSelect } from './header/TargetSelect';
import { ModeSelect } from './header/ModeSelect';
import { Loader } from './shared/Loader';
import { CodeView, LinkedCodeRange } from './results/CodeView';
import { AstView } from './results/AstView';
import { VerifyView } from './results/VerifyView';
import { ExplainView } from './results/ExplainView';
import { OutputView } from './results/OutputView';

type ParsedRunResult = Cacheable<Omit<RunResult, 'value'> & {
    value: Exclude<RunResult['value'], string>;
}>;
type ParsedResult = Exclude<Result, RunResult>|ParsedRunResult;

type Props = {
    options: AppOptions;
    result: ParsedResult;
    selectedCodeOffset?: number;
    // TODO: Consolidate
    onAstSelect: (item: AstItem | null) => void;
    onCodeRangeSelect: (range: LinkedCodeRange | null) => void;
};

type CodeState = Pick<CodeResult, 'value'|'ranges'> & { language: TargetLanguageName };

const EMPTY_OUTPUT = [] as ReadonlyArray<OutputItem>;
export const ResultsSection: FC<Props> = ({ options, result, selectedCodeOffset, onAstSelect, onCodeRangeSelect }) => {
    const [lastCodeState, setLastCodeState] = useState<CodeState>();

    // Code is special since CodeMirror is slow to set up, so we hide it instead of destroying it
    useEffect(() => {
        if (result.type === 'code')
            setLastCodeState({ ...result, language: options.target as TargetLanguageName });
    // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [result]);
    const codeResult = lastCodeState && <div hidden={result.type !== 'code'}>
        <CodeView
            code={lastCodeState.value ?? ''}
            ranges={lastCodeState.ranges}
            language={lastCodeState.language}
            onRangeSelect={onCodeRangeSelect} />
    </div>;

    const renderNonCodeResult = () => {
        switch (result.type) {
            case 'code':
                return null;
            case 'ast':
                return <AstView roots={result.value} onSelect={onAstSelect} selectedOffset={selectedCodeOffset} />;
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
            <TargetSelect
                target={options.target}
                onSelect={t => options.target = t}
                htmlProps={{ tabIndex: 4 }}
            />
            {result.cached && <small className="result-cached-indicator" title="Note: This output is cached and might not represent the latest behavior">
                Cached: {dateFormat(result.cached.date, 'd mmm yyyy')}
            </small>}
            <ModeSelect
                mode={options.release ? 'release' : 'debug'}
                onSelect={m => options.release = (m === 'release')}
                htmlProps={{ tabIndex: 5 }}
            />
        </header>
        <div className="content">
            <Loader />
            {codeResult}
            {renderNonCodeResult()}
        </div>
    </section>;
};
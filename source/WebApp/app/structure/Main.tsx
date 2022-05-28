import React from 'react';
import { useRecoilValue, useSetRecoilState } from 'recoil';
import { MobileSettings } from '../features/mobile-settings/MobileSettings';
import { BranchDetailsSection } from '../features/roslyn-branches/BranchDetailsSection';
import { classNames } from '../shared/helpers/classNames';
import type { UpdateResult } from '../shared/resultTypes';
import { codeState } from '../shared/state/codeState';
import { initialCodeState } from '../shared/state/initialCodeState';
import { onlineState } from '../shared/state/onlineState';
import { useDispatchResultUpdate, resultSelector } from '../shared/state/resultState';
import { targetOptionState } from '../shared/state/targetOptionState';
import { CodeEditor } from './code/CodeEditor';
import { CodeSection } from './CodeSection';
import { ErrorsSection } from './ErrorsSection';
import { ResultsSection } from './ResultsSection';
import { useLoadingWait } from './useLoadingWait';
import { WarningsSection } from './WarningsSection';

const EMPTY_ARRAY = [] as ReadonlyArray<never>;
export const Main: React.FC = () => {
    const initialCode = useRecoilValue(initialCodeState);
    const setCode = useSetRecoilState(codeState);
    const target = useRecoilValue(targetOptionState);
    const setOnline = useSetRecoilState(onlineState);
    const { loading, onWait, endWait } = useLoadingWait();
    const dispatchResultUpdate = useDispatchResultUpdate();
    const result = useRecoilValue(resultSelector);

    const onServerError = (message: string) => dispatchResultUpdate({ type: 'serverError', message });
    const onSlowUpdateResult = (updateResult: UpdateResult) => {
        endWait();
        dispatchResultUpdate({
            type: 'updateResult', updateResult, target
        });
    };

    const codeEditor = <CodeEditor
        initialCode={initialCode}
        initialCached={!!result?.cached}
        executionFlow={(result?.type === 'run' && result.value) ? result.value.flow : null}
        onCodeChange={get => setCode(get())}
        onConnectionChange={s => setOnline(s === 'open')}
        onServerError={onServerError}
        onSlowUpdateResult={onSlowUpdateResult}
        onSlowUpdateWait={onWait} />;

    // Main does not actually output <main> tag, as React does not recommend
    // attaching <App> (its parent) to <body> -- so its parent is already <main>.
    return <>
        <MobileSettings buttonProps={{ tabIndex: 1 }} />
        <div className="mobile-offline-notice">connection lost, reconnecting…</div>

        <div className="top-section-group top-section-group-code">
            <CodeSection codeEditor={codeEditor} />
            <BranchDetailsSection className="top-section" />
        </div>
        <div className={classNames('top-section-group top-section-group-results', loading && 'loading')}>
            <ResultsSection />
            <ErrorsSection errors={result?.errors ?? EMPTY_ARRAY} />
            <WarningsSection warnings={result?.warnings ?? EMPTY_ARRAY} />
        </div>
    </>;
};
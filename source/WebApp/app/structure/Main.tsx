import React, { FC } from 'react';
import { useRecoilValue, useSetRecoilState } from 'recoil';
import { MobileSettings } from '../features/mobile-settings/MobileSettings';
import { BranchDetailsSection } from '../features/roslyn-branches/BranchDetailsSection';
import { classNames } from '../helpers/classNames';
import type { UpdateResult } from '../shared/resultTypes';
import { codeState } from '../shared/state/codeState';
import { initialCodeState } from '../shared/state/initialCodeState';
import { onlineState } from '../shared/state/onlineState';
import { useDispatchResultUpdate, resultSelector } from '../shared/state/resultState';
import { statusSelector } from '../shared/state/statusSelector';
import { targetOptionState } from '../shared/state/targetOptionState';
import { CodeEditor } from './code/CodeEditor';
import { CodeTopSection } from './CodeTopSection';
import { ErrorsTopSection } from './ErrorsTopSection';
import { ResultsTopSection } from './ResultsTopSection';
import { useLoadingWait } from './useLoadingWait';
import { WarningsTopSection } from './WarningsTopSection';

const EMPTY_ARRAY = [] as ReadonlyArray<never>;
export const Main: FC = () => {
    const initialCode = useRecoilValue(initialCodeState);
    const setCode = useSetRecoilState(codeState);
    const target = useRecoilValue(targetOptionState);
    const setOnline = useSetRecoilState(onlineState);
    const { loading, onWait, endWait } = useLoadingWait();
    const dispatchResultUpdate = useDispatchResultUpdate();
    const result = useRecoilValue(resultSelector);
    const status = useRecoilValue(statusSelector);

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

    const className = `root-status-${status}` as const;
    return <main className={className}>
        <MobileSettings buttonProps={{ tabIndex: 1 }} />
        <div className="mobile-offline-notice">connection lost, reconnecting…</div>

        <div className="top-section-group top-section-group-code">
            <CodeTopSection codeEditor={codeEditor} />
            <BranchDetailsSection className="top-section" />
        </div>
        <div className={classNames('top-section-group top-section-group-results', loading && 'loading')}>
            <ResultsTopSection />
            <ErrorsTopSection errors={result?.errors ?? EMPTY_ARRAY} />
            <WarningsTopSection warnings={result?.warnings ?? EMPTY_ARRAY} />
        </div>
    </main>;
};
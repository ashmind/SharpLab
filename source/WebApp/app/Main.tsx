import React, { FC, useContext, useState } from 'react';
import type { Result, UpdateResult } from '../ts/types/results';
import type { Gist } from '../ts/types/gist';
import { CodeEditor } from './code/CodeEditor';
import { GistManager, GistManagerProps } from './header/GistManager';
import { MobileSettings } from './mobile/MobileSettings';
import { useOption } from './shared/useOption';
import { useResult, useDispatchResultUpdate } from './shared/useResult';
import { classNames } from './helpers/classNames';
import { ErrorsTopSection } from './ErrorsTopSection';
import { ResultsTopSection } from './ResultsTopSection';
import { WarningsTopSection } from './WarningsTopSection';
import { BranchDetailsSection } from './code/BranchDetailsSection';
import { useAndSetCode } from './shared/useCode';
import { CodeRangeSyncProvider } from './main/CodeRangeSyncProvider';
import { useLoadingWait } from './main/useLoadingWait';
import { InitialCodeContext } from './main/AppStateManager';
import { CodeTopSection } from './CodeTopSection';

const getStatus = (online: boolean, result: Result | undefined) => {
    if (!online)
        return 'offline';

    const error = !!(result && !result.success);
    return error ? 'error' : 'default';
};

const EMPTY_ARRAY = [] as ReadonlyArray<never>;
export const Main: FC = () => {
    const initialCode = useContext(InitialCodeContext);
    const [, setCode] = useAndSetCode();
    const branch = useOption('branch');
    const target = useOption('target');
    const [online, setOnline] = useState(true);
    const { loading, onWait, endWait } = useLoadingWait();
    const dispatchResultUpdate = useDispatchResultUpdate();
    const result = useResult();
    const [gist, setGist] = useState<Gist | null>(null);

    const status = getStatus(online, result);

    const getGistManager = (props: Omit<GistManagerProps, 'context'|'gist'|'onSave'> = {}) => <GistManager
        gist={gist}
        onSave={setGist}
        {...props}
    />;

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
        <MobileSettings
            buttonProps={{ tabIndex: 1 }}
            gistManager={getGistManager({ useLabel: true })} />
        <div className="mobile-offline-notice">connection lost, reconnecting…</div>

        <CodeRangeSyncProvider>
            <div className="top-section-group top-section-group-code">
                <CodeTopSection codeEditor={codeEditor} getGistManager={getGistManager} />
                {branch && <BranchDetailsSection branch={branch} className="top-section" />}
            </div>
            <div className={classNames('top-section-group top-section-group-results', loading && 'loading')}>
                <ResultsTopSection />
                <ErrorsTopSection errors={result?.errors ?? EMPTY_ARRAY} />
                <WarningsTopSection warnings={result?.warnings ?? EMPTY_ARRAY} />
            </div>
        </CodeRangeSyncProvider>
    </main>;
};
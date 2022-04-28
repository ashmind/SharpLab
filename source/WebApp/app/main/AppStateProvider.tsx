import { useAsync } from 'app/helpers/useAsync';
import { ResultContext } from 'app/shared/contexts/ResultContext';
import { optionContexts, OptionName } from 'app/shared/contexts/optionContexts';
import React, { ReactNode, useEffect, useMemo, useReducer, useState } from 'react';
import getBranchesAsync from 'ts/server/get-branches-async';
import { AppStateData, loadStateAsync } from 'ts/state/state';
import type { AppOptions } from 'ts/types/app';
import { resolveBranchAsync } from 'ts/ui/branches';
import { CodeContext } from 'app/shared/contexts/CodeContext';
import { BranchesContext } from 'app/shared/contexts/BranchesContext';
import type { Branch } from 'ts/types/branch';
import type { CachedUpdateResult } from 'ts/types/results';
import { MutableValueProvider } from './state/MutableValueProvider';
import { resultReducer } from './state/resultReducer';

const EMPTY_BRANCHES = [] as ReadonlyArray<Branch>;
export const AppStateProvider = ({ children }: { children: ReactNode }) => {
    const [options, setOptions] = useState<AppOptions>();
    const [code, setCode] = useState<string>('');
    // eslint-disable-next-line no-undefined
    const [result, dispatchResultAction] = useReducer(resultReducer, undefined);
    const resultContext = useMemo(() => [result, dispatchResultAction] as const, [result]);

    const [startBranchesLoad, branches] = useAsync(getBranchesAsync, []);
    const [startStateLoad, loadedState] = useAsync(async () => {
        const state = {} as Partial<AppStateData>;
        const setResultFromCache = (updateResult: CachedUpdateResult, { target }: AppOptions) => dispatchResultAction({
            type: 'cachedResult', updateResult, target
        });

        await loadStateAsync(state, { resolveBranchAsync, setResultFromCache });
        return state as AppStateData;
    }, []);

    useEffect(() => {
        startBranchesLoad();
        startStateLoad();
    // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    useEffect(() => {
        if (!loadedState)
            return;
        setOptions(loadedState.options);
        setCode(loadedState.code);
    }, [loadedState]);

    if (!options)
        return null;

    const renderOptionProviders = (children: ReactNode) => {
        const optionNames = Object.keys(options) as ReadonlyArray<(keyof typeof options)>;

        return optionNames.reduce(<TName extends OptionName>(children: ReactNode, name: TName) => <MutableValueProvider
            context={optionContexts[name]}
            value={options[name]}
            // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
            setValue={value => setOptions(options => ({ ...options!, [name]: value }))}
        >{children}</MutableValueProvider>, children);
    };

    return <MutableValueProvider context={CodeContext} value={code} setValue={setCode}>
        <BranchesContext.Provider value={branches ?? EMPTY_BRANCHES}>
            {renderOptionProviders(<ResultContext.Provider value={resultContext}>
                {children}
            </ResultContext.Provider>)}
        </BranchesContext.Provider>
    </MutableValueProvider>;
};
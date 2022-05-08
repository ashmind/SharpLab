import React, { createContext, ReactNode, useEffect, useMemo, useReducer, useState } from 'react';
import { useRecoilState } from 'recoil';
import getBranchesAsync from '../../ts/server/get-branches-async';
import defaults from '../../ts/state/handlers/defaults';
import { AppStateData, loadStateAsync } from '../../ts/state/state';
import type { AppOptions } from '../../ts/types/app';
import type { Branch } from '../../ts/types/branch';
import type { CachedUpdateResult } from '../../ts/types/results';
import { resolveBranchAsync } from '../../ts/ui/branches';
import { gistState } from '../features/save-as-gist/gistState';
import { useAsync } from '../helpers/useAsync';
import { BranchesContext } from '../shared/contexts/BranchesContext';
import { CodeContext } from '../shared/contexts/CodeContext';
import { OptionName, optionContexts } from '../shared/contexts/optionContexts';
import { ResultContext } from '../shared/contexts/ResultContext';
import { MutableValueProvider } from './state/MutableValueProvider';
import { resultReducer } from './state/resultReducer';

export const InitialCodeContext = createContext<string>('');

const EMPTY_BRANCHES = [] as ReadonlyArray<Branch>;
export const AppStateManager = ({ children }: { children: ReactNode }) => {
    const [options, setOptions] = useState<AppOptions>();
    const [initialCode, setInitialCode] = useState<string>('');
    const [code, setCode] = useState<string>('');
    // TODO: This should be moved into the Gist feature for clearer responsibility split
    const [, setGist] = useRecoilState(gistState);
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
        setInitialCode(loadedState.code);
        setCode(loadedState.code);
        setGist(loadedState.gist);
    }, [loadedState, setGist]);

    useEffect(() => {
        if (!options)
            return;
        const { language, target } = options;
        const loaded = loadedState?.options;
        if (language !== loaded?.language || target !== loaded.target)
            setInitialCode(defaults.getCode(language, target));
    }, [loadedState, options]);

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

    return <InitialCodeContext.Provider value={initialCode}>
        <MutableValueProvider context={CodeContext} value={code} setValue={setCode}>
            <BranchesContext.Provider value={branches ?? EMPTY_BRANCHES}>
                {renderOptionProviders(<ResultContext.Provider value={resultContext}>
                    {children}
                </ResultContext.Provider>)}
            </BranchesContext.Provider>
        </MutableValueProvider>
    </InitialCodeContext.Provider>;
};
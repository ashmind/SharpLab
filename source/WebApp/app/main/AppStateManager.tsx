import React, { createContext, ReactNode, useEffect, useMemo, useReducer, useState } from 'react';
import { useRecoilState } from 'recoil';
import defaults from '../../ts/state/handlers/defaults';
import { loadedStatePromise, saveState } from '../../ts/state/state';
import { branchOptionState } from '../features/roslyn-branches/branchOptionState';
import { gistState } from '../features/save-as-gist/gistState';
import { ResultContext } from '../shared/contexts/ResultContext';
import { codeState } from '../shared/state/codeState';
import { languageOptionState } from '../shared/state/languageOptionState';
import { releaseOptionState } from '../shared/state/releaseOptionState';
import { targetOptionState } from '../shared/state/targetOptionState';
import { resultReducer } from './state/resultReducer';

export const InitialCodeContext = createContext<string>('');

export const AppStateManager = ({ children }: { children: ReactNode }) => {
    const [loaded, setLoaded] = useState(false);
    const [language, setLanguage] = useRecoilState(languageOptionState);
    const [branch, setBranch] = useRecoilState(branchOptionState);
    const [target, setTarget] = useRecoilState(targetOptionState);
    const [release, setRelease] = useRecoilState(releaseOptionState);
    const [initialCode, setInitialCode] = useState<string>('');
    const [code, setCode] = useRecoilState(codeState);
    // TODO: This should be moved into the Gist feature for clearer responsibility split
    const [gist, setGist] = useRecoilState(gistState);
    // eslint-disable-next-line no-undefined
    const [result, dispatchResultAction] = useReducer(resultReducer, undefined);
    const resultContext = useMemo(() => [result, dispatchResultAction] as const, [result]);

    useEffect(() => {
        void((async () => {
            const {
                options: { language, branch, target, release },
                code,
                gist,
                cachedResult
            } = await loadedStatePromise;

            setLanguage(language);
            setBranch(branch);
            setTarget(target);
            setRelease(release);
            setInitialCode(code);
            setCode(code);
            setGist(gist);

            if (cachedResult)
                dispatchResultAction({ type: 'cachedResult', updateResult: cachedResult, target });

            setLoaded(true);
        })());
    // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    useEffect(() => {
        if (!loaded)
            return;
        setInitialCode(defaults.getCode(language, target));
    }, [loaded, language, target]);

    useEffect(() => {
        if (!loaded)
            return;
        saveState({ code, options: { language, branch, target, release }, gist });
    }, [loaded, language, branch, target, release, code, gist]);

    if (!loaded)
        return null;

    return <InitialCodeContext.Provider value={initialCode}>
        <ResultContext.Provider value={resultContext}>
            {children}
        </ResultContext.Provider>
    </InitialCodeContext.Provider>;
};
import React, { FC, ReactNode, useEffect, useState } from 'react';
import { useRecoilState, useSetRecoilState } from 'recoil';
import defaults from '../../ts/state/handlers/defaults';
import { loadedStatePromise, saveState } from '../../ts/state/state';
import { branchOptionState } from '../features/roslyn-branches/branchOptionState';
import { gistState } from '../features/save-as-gist/gistState';
import { codeState } from '../shared/state/codeState';
import { initialCodeState } from '../shared/state/initialCodeState';
import { languageOptionState } from '../shared/state/languageOptionState';
import { releaseOptionState } from '../shared/state/releaseOptionState';
import { useDispatchResultUpdate } from '../shared/state/resultState';
import { targetOptionState } from '../shared/state/targetOptionState';

type Props = {
    children: ReactNode;
};

export const AppStateManager: FC<Props> = ({ children }) => {
    const [loaded, setLoaded] = useState(false);
    const [language, setLanguage] = useRecoilState(languageOptionState);
    const [branch, setBranch] = useRecoilState(branchOptionState);
    const [target, setTarget] = useRecoilState(targetOptionState);
    const [release, setRelease] = useRecoilState(releaseOptionState);
    const setInitialCode = useSetRecoilState(initialCodeState);
    const [code, setCode] = useRecoilState(codeState);
    // TODO: This should be moved into the Gist feature for clearer responsibility split
    const [gist, setGist] = useRecoilState(gistState);
    // eslint-disable-next-line no-undefined
    const dispatchResultUpdate = useDispatchResultUpdate();

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
                dispatchResultUpdate({ type: 'cachedResult', updateResult: cachedResult, target });

            setLoaded(true);
        })());
    // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    useEffect(() => {
        if (!loaded)
            return;
        setInitialCode(defaults.getCode(language, target));
    }, [loaded, language, target, setInitialCode]);

    useEffect(() => {
        if (!loaded)
            return;
        saveState({ code, options: { language, branch, target, release }, gist });
    }, [loaded, language, branch, target, release, code, gist]);

    if (!loaded)
        return null;

    return <>{children}</>;
};
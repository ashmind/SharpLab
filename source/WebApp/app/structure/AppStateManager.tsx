import { useEffect } from 'react';
import { useRecoilState, useSetRecoilState } from 'recoil';
import { loadedStatePromise, saveState } from '../features/persistent-state/state';
import { branchOptionState } from '../features/roslyn-branches/branchOptionState';
import { gistState } from '../features/save-as-gist/gistState';
import { getDefaultCode } from '../shared/defaults';
import { codeState } from '../shared/state/codeState';
import { initialCodeState } from '../shared/state/initialCodeState';
import { languageOptionState } from '../shared/state/languageOptionState';
import { releaseOptionState } from '../shared/state/releaseOptionState';
import { useDispatchResultUpdate } from '../shared/state/resultState';
import { targetOptionState } from '../shared/state/targetOptionState';
import { appLoadedState } from './appLoadedState';

export const AppStateManager: React.FC = () => {
    const [loaded, setLoaded] = useRecoilState(appLoadedState);
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
        setInitialCode(getDefaultCode(language, target));
    }, [loaded, language, target, setInitialCode]);

    useEffect(() => {
        if (!loaded)
            return;
        saveState([
            ['options', [language, branch, target, release]],
            ['code', code],
            ['gist', gist]
        ]);
    }, [loaded, language, branch, target, release, code, gist]);

    return null;
};
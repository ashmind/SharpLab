import { DEFAULT_LANGUAGE, DEFAULT_RELEASE, DEFAULT_TARGET, getDefaultCode } from '../../shared/defaults';
import type { LanguageName } from '../../shared/languages';
import type { TargetName } from '../../shared/targets';
import { type CacheKeyData, loadResultFromCacheAsync } from '../result-cache/cacheLogic';
import { resolveBranchAsync } from '../roslyn-branches/resolveBranchAsync';
import type { Branch } from '../roslyn-branches/types';
import type { Gist } from '../save-as-gist/Gist';
import { toOptionsData } from './handlers/helpers/optionsData';
import lastUsed from './handlers/last-used';
import { saveStateToUrl, loadStateFromUrlAsync } from './handlers/url';

// Cannot use objects for these, since in TypeScript
// there is no way to constrain an object to ensure it only has
// expected properties -- and any unexpected properties
// will not be saved.
type AppStateTuple = readonly [
    readonly [name: 'options', value: readonly [
        language: LanguageName,
        branch: Branch | null,
        target: TargetName,
        release: boolean
    ]],
    readonly [name: 'code', value: string],
    readonly [name: 'gist', value: Gist | null],
];

const stateMatches = (saved: AppStateTuple, current: AppStateTuple) => {
    const [[, savedOptions], ...savedRest] = saved;
    const [[, currentOptions], ...currentRest] = current;

    return currentOptions.every((value, index) => value === savedOptions[index])
        && currentRest.every(([, value], index) => value === savedRest[index][1]);
};

let lastSavedState: AppStateTuple | undefined;
export const saveState = (state: AppStateTuple) => {
    if (!lastSavedState)
        throw new Error('Attempted to save state before load');

    if (stateMatches(lastSavedState, state))
        return;

    const [[, options], [, code], [, gist]] = state;
    const optionsData = toOptionsData(...options);

    lastUsed.saveOptions(optionsData);
    const { keepGist } = saveStateToUrl(code, optionsData, { gist });
    lastSavedState = state;
    return { gist: keepGist ? gist : null };
};

const loadResultFromCacheSafeAsync = async (cacheKey: CacheKeyData) => {
    try {
        return await loadResultFromCacheAsync(cacheKey);
    }
    catch (e) {
        console.warn('Failed to load cached result: ', e);
        return null;
    }
};

const loadStateAsync = async () => {
    const fromUrl = await loadStateFromUrlAsync();
    const lastUsedOptions = lastUsed.loadOptions();

    const loadedOptions = fromUrl?.options ?? lastUsedOptions ?? {};

    const language = loadedOptions.language ?? DEFAULT_LANGUAGE;
    const target = loadedOptions.target ?? DEFAULT_TARGET;
    const release = loadedOptions.release ?? DEFAULT_RELEASE;
    let branchId = loadedOptions.branchId ?? null;
    if (branchId === 'master')
        branchId = 'main';

    const branch = branchId ? (await resolveBranchAsync(branchId)) : null;
    const options = { language, target, release, branch };

    const code = fromUrl?.code ?? getDefaultCode(language, target);

    const gist = fromUrl && ('gist' in fromUrl)
        ? fromUrl.gist
        : null;

    const cachedResult = await loadResultFromCacheSafeAsync({
        language,
        target,
        release,
        branchId,
        code
    });
    if (lastUsedOptions && !fromUrl?.options) {
        // need to re-sync implicit options into URL
        saveStateToUrl(fromUrl?.code, toOptionsData(language, branch, target, release));
    }

    return {
        options,
        code,
        gist,
        cachedResult
    };
};

export const loadedStatePromise = loadStateAsync().then(s => {
    lastSavedState = [
        ['options', [s.options.language, s.options.branch, s.options.target, s.options.release]],
        ['code', s.code],
        ['gist', s.gist]
    ];
    return s;
});
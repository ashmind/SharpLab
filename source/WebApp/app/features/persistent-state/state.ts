import toRawOptions from '../../../ts/helpers/to-raw-options';
import { DEFAULT_OPTIONS, getDefaultCode } from '../../shared/defaults';
import type { LanguageName } from '../../shared/languages';
import type { TargetName } from '../../shared/targets';
import { type CacheKeyData, loadResultFromCacheAsync } from '../result-cache/cacheLogic';
import { resolveBranchAsync } from '../roslyn-branches/resolveBranchAsync';
import type { Branch } from '../roslyn-branches/types';
import type { Gist } from '../save-as-gist/gist';
import lastUsed from './handlers/last-used';
import { saveStateToUrl, loadStateFromUrlAsync } from './handlers/url';

type ExactMatch<TA, TB> = TA extends TB
    ? (TB extends TA ? TA : never)
    : never;

type OptionsData = {
    language: LanguageName;
    target: TargetName;
    release: boolean;
    branch: Branch | null;
};

export type StateData = {
    options: OptionsData;
    code: string;
    gist: Gist | null;
};

type ExactStateData<TOptions> = StateData & {
    options: ExactMatch<TOptions, OptionsData>;
};

let lastSavedState: StateData|undefined;
const stateMatches = (left: StateData, right: StateData) => {
    return left.options.language === right.options.language
        && left.options.branch === right.options.branch
        && left.options.target === right.options.target
        && left.options.release === right.options.release
        && left.code === right.code
        && left.gist === right.gist;
};

export const saveState = <TOptions>(state: ExactStateData<TOptions>) => {
    if (!lastSavedState)
        throw new Error('Attempted to save state before load');

    if (stateMatches(lastSavedState, state))
        return;

    const { code, options, gist } = state;
    const rawOptions = toRawOptions(options);

    lastUsed.saveOptions(rawOptions);
    const { keepGist } = saveStateToUrl(code, rawOptions, { gist });
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

    const language = loadedOptions.language ?? DEFAULT_OPTIONS.language;
    const target = loadedOptions.target ?? DEFAULT_OPTIONS.target;
    const release = loadedOptions.release ?? DEFAULT_OPTIONS.release;
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
    if (lastUsedOptions && !fromUrl?.options) // need to re-sync implicit options into URL
        saveStateToUrl(fromUrl?.code, toRawOptions(options));

    return {
        options,
        code,
        gist,
        cachedResult
    };
};

export const loadedStatePromise = loadStateAsync().then(s => {
    lastSavedState = s;
    return s;
});
import type { AppData } from '../types/app';
import type { Branch } from '../types/branch';
import toRawOptions from '../helpers/to-raw-options';
import defaults from './handlers/defaults';
import lastUsed from './handlers/last-used';
import { saveStateToUrl, loadStateFromUrlAsync } from './handlers/url';

type AppStateData = {
    options: AppData['options'],
    code: AppData['code'],
    gist: AppData['gist'],
    cache: {
        secret: AppData['cache']['secret']
    }
};

export default {
    save(state: AppStateData) {
        const { code, options, gist, cache } = state;
        const rawOptions = toRawOptions(options);

        lastUsed.saveOptions(rawOptions);
        const { keepGist } = saveStateToUrl(code, rawOptions, { gist, cacheSecret: cache.secret });
        if (!keepGist)
            state.gist = null;
    },

    async loadAsync(
        state: Partial<AppStateData> & Required<Pick<AppStateData, 'cache'>>,
        resolveBranchAsync: (id: string) => Promise<Branch|null>
    ) {
        const fromUrl = await loadStateFromUrlAsync();
        const lastUsedOptions = lastUsed.loadOptions();

        const loadedOptions = fromUrl?.options ?? lastUsedOptions ?? {};
        const defaultOptions = defaults.getOptions();

        const language = loadedOptions.language ?? defaultOptions.language;
        const target = loadedOptions.target ?? defaultOptions.target;
        const release = loadedOptions.release ?? defaultOptions.release;
        let branchId = loadedOptions.branchId ?? null;
        if (branchId === 'master')
            branchId = 'main';

        const branch = branchId ? (await resolveBranchAsync(branchId)) : null;
        const options = { language, target, release, branch };

        const code = fromUrl?.code ?? defaults.getCode(language, target);

        state.options = options;
        state.code = code;
        state.gist = fromUrl && ('gist' in fromUrl)
            ? fromUrl.gist
            : null;
        if (fromUrl && 'cacheSecret' in fromUrl)
            state.cache.secret = fromUrl.cacheSecret;

        if (lastUsedOptions && !fromUrl?.options) // need to re-sync implicit options into URL
            await saveStateToUrl(fromUrl?.code, toRawOptions(options), { cacheSecret: state.cache.secret });
    }
};
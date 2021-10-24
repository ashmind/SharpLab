import type { Branch } from '../types/branch';
import type { Gist } from '../types/gist';
import toRawOptions from '../helpers/to-raw-options';
import type { App } from '../types/app';
import defaults from './handlers/defaults';
import lastUsed from './handlers/last-used';
import { loadStateFromUrlAsync, saveStateToUrl, StateLoadedFromUrl } from './handlers/url';

type AppStateKey = 'options'|'code'|'gist';
export type AppState = Pick<App, AppStateKey>;

export default {
    save(state: AppState) {
        const { code, options, gist } = state;
        const rawOptions = toRawOptions(options);

        lastUsed.saveOptions(rawOptions);
        const { keepGist } = saveStateToUrl(code, rawOptions, { gist });
        if (!keepGist)
            state.gist = null;
    },

    async loadAsync(state: Partial<AppState>, resolveBranch: (id: string) => Promise<Branch|null>) {
        const fromUrl = (await loadStateFromUrlAsync()) ?? {} as Partial<StateLoadedFromUrl>;
        const lastUsedOptions = lastUsed.loadOptions();

        const loadedOptions = fromUrl.options ?? lastUsedOptions ?? {} as Partial<Exclude<StateLoadedFromUrl['options'], undefined>>;
        const defaultOptions = defaults.getOptions();

        const language = loadedOptions.language ?? defaultOptions.language;
        const target = loadedOptions.target ?? defaultOptions.target;
        const release = loadedOptions.release ?? defaultOptions.release;
        let branchId = loadedOptions.branchId ?? null;
        if (branchId === 'master')
            branchId = 'main';

        const branch = branchId ? (await resolveBranch(branchId)) : null;
        const options = { language, target, release, branch };

        const code = fromUrl.code ?? defaults.getCode(language, target);

        state.options = options;
        state.code = code;
        state.gist = (fromUrl as { gist?: Gist }).gist;
        if (lastUsedOptions && !fromUrl.options) // need to re-sync implicit options into URL
            saveStateToUrl(fromUrl.code, toRawOptions(options));
    }
};
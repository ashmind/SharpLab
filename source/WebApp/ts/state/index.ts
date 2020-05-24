import type { App } from '../types/app';
import type { Branch } from '../types/branch';
import type { Gist } from '../types/gist';
import toRawOptions from '../helpers/to-raw-options';
import defaults from './handlers/defaults';
import lastUsed from './handlers/last-used';
import url from './handlers/url';

type AppStateKey = 'options'|'code'|'gist';
type UnwrapPromise<U> = U extends Promise<infer T> ? T : never;

export default {
    save(state: Pick<App, AppStateKey>) {
        const { code, options, gist } = state;
        const rawOptions = toRawOptions(options);

        lastUsed.saveOptions(rawOptions);
        const { keepGist } = url.save(code, rawOptions, { gist });
        if (!keepGist)
            state.gist = null;
    },

    async loadAsync(state: Partial<Pick<App, AppStateKey>>, resolveBranch: (id: string) => Promise<Branch|null>) {
        const fromUrl = (await url.loadAsync()) ?? {} as Partial<UnwrapPromise<ReturnType<typeof url.loadAsync>>>;
        const lastUsedOptions = lastUsed.loadOptions();

        const loadedOptions = fromUrl.options ?? lastUsedOptions ?? {};
        const defaultOptions = defaults.getOptions();

        const language = loadedOptions.language ?? defaultOptions.language;
        const target = loadedOptions.target ?? defaultOptions.target;
        const release = loadedOptions.release ?? defaultOptions.release;
        const branchId = loadedOptions.branchId ?? null;

        const branch = branchId ? (await resolveBranch(branchId)) : null;
        const options = { language, target, release, branch };

        const code = fromUrl.code ?? defaults.getCode(language, target);

        state.options = options;
        state.code = code;
        state.gist = (fromUrl as { gist?: Gist }).gist;
        if (lastUsedOptions && !fromUrl.options) // need to re-sync implicit options into URL
            url.save(fromUrl.code, toRawOptions(options));
    }
};
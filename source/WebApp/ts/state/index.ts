import type { App, AppOptions } from '../types/app';
import type { Gist } from '../types/gist';
import defaults from './handlers/defaults';
import lastUsed from './handlers/last-used';
import url from './handlers/url';

type AppStateKey = 'options'|'code'|'gist';

export default {
    save(state: { [Key in AppStateKey]: App[Key] }) {
        lastUsed.saveOptions(state.options);
        const { keepGist } = url.save(state.code, state.options, { gist: state.gist });
        if (!keepGist)
            state.gist = null;
    },

    async loadAsync(state: { [Key in AppStateKey]?: App[Key] }) {
        const fromUrl = (await url.loadAsync()) ?? {} as {
            [Key in AppStateKey]?: App[Key]
        };
        const lastUsedOptions = lastUsed.loadOptions();

        const options = fromUrl.options ?? lastUsedOptions ?? {};
        const defaultOptions = defaults.getOptions();
        for (const key of Object.keys(defaultOptions) as ReadonlyArray<keyof typeof defaultOptions>) {
            if (options[key] == null) {
                // eslint-disable-next-line @typescript-eslint/no-explicit-any
                options[key] = defaultOptions[key] as any;
            }
        }
        const code = fromUrl.code ?? defaults.getCode(options.language, options.target);

        state.options = options as AppOptions;
        state.code = code;
        state.gist = (fromUrl as { gist?: Gist }).gist;
        if (options === lastUsedOptions) // need to re-sync implicit options into URL
            url.save(fromUrl.code, state.options);
    }
};
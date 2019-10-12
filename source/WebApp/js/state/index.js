import defaults from './handlers/defaults.js';
import lastUsed from './handlers/last-used.js';
import url from './handlers/url.js';

export default {
    save: state => {
        lastUsed.saveOptions(state.options);
        const { keepGist } = url.save(state.code, state.options, { gist: state.gist });
        if (!keepGist)
            state.gist = null;
    },

    loadAsync: async state => {
        const fromUrl = /** @type {UnwrapPromise<ReturnType<typeof url.loadAsync>>} */((await url.loadAsync()) || {});
        const lastUsedOptions = lastUsed.loadOptions();

        const options = fromUrl.options || lastUsedOptions || {};
        const defaultOptions = defaults.getOptions();
        for (const key of Object.keys(defaultOptions)) {
            if (options[key] == null)
                options[key] = defaultOptions[key];
        }
        const code = fromUrl.code || defaults.getCode(options.language, options.target);

        state.options = options;
        state.code = code;
        state.gist = fromUrl.gist;
        if (options === lastUsedOptions) // need to re-sync implicit options into URL
            url.save(fromUrl.code, state.options);
    }
};
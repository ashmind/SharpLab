import defaults from './handlers/defaults.js';
import lastUsed from './handlers/last-used.js';
import url from './handlers/url.js';

export default {
    save: state => {
        lastUsed.saveOptions(state.options);
        url.save(state.code, state.options);
    },

    loadAsync: async state => {
        const fromUrl = (await url.loadAsync()) || {};
        const lastUsedOptions = lastUsed.loadOptions();

        const options = fromUrl.options || lastUsedOptions || {};
        const defaultOptions = defaults.getOptions();
        for (const key of Object.keys(defaultOptions)) {
            if (options[key] == null)
                options[key] = defaultOptions[key];
        }
        const code = fromUrl.code || defaults.getCode(options.language);

        state.options = options;
        state.code = code;
        if (options === lastUsedOptions) // need to resync implicit options into URL
            url.save(fromUrl.code, state.options);
    }
};
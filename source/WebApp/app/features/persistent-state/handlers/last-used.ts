import type { RawOptions } from '../../../../ts/types/raw-options';

const version = 3 as const;
type OptionsV3 = {
    version: 3;
    options: RawOptions;
};

export default {
    loadOptions() {
        const loaded = localStorage['sharplab.options'] || localStorage['tryroslyn.options'];
        if (!loaded)
            return null;

        try {
            const parsed = JSON.parse(loaded) as OptionsV3 | { version?: 1|2 };
            if (parsed.version !== version)
                return null;
            return parsed.options;
        }
        catch (ex) {
            console.warn('Failed to load options:', ex);
            return null;
        }
    },

    saveOptions(options: RawOptions) {
        try {
            localStorage['sharplab.options'] = JSON.stringify({ version, options });
        }
        catch (ex) {
            console.warn('Failed to save options:', ex);
        }
    }
};
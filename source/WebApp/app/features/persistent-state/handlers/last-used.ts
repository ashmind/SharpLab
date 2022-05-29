import type { ExactOptionsData, OptionsData } from './helpers/optionsData';

const version = 3 as const;
type OptionsV3 = {
    version: 3;
    options: OptionsData;
};
type OptionsV3ForSave = {
    version: 3;
    options: ExactOptionsData;
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

    // Cannot use objects for these, since in TypeScript
    // there is no way to constrain an object to ensure it only has
    // expected properties -- and any unexpected properties
    // will not be saved.
    saveOptions(options: ExactOptionsData) {
        const data: OptionsV3ForSave = { version, options };
        try {
            localStorage['sharplab.options'] = JSON.stringify(data);
        }
        catch (ex) {
            console.warn('Failed to save options:', ex);
        }
    }
};
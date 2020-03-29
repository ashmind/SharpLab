import type { AppOptions } from '../../types/app';
import extendType from '../../helpers/extend-type';
import warn from '../../helpers/warn';

const version = 3;
export default {
    loadOptions: function() {
        const loaded = localStorage['sharplab.options'] || localStorage['tryroslyn.options'];
        if (!loaded)
            return null;

        try {
            const parsed = JSON.parse(loaded) as { version?: number };
            if (parsed.version !== version)
                return null;
            return (extendType(parsed)<{ options: AppOptions }>()).options;
        }
        catch (ex) {
            warn('Failed to load options:', ex);
            return null;
        }
    },

    saveOptions: function(options: AppOptions) {
        try {
            localStorage['sharplab.options'] = JSON.stringify({ version, options });
        }
        catch (ex) {
            warn('Failed to save options:', ex);
        }
    }
};
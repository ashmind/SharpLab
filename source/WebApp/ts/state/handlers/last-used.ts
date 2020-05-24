import extendType from '../../helpers/extend-type';
import warn from '../../helpers/warn';
import type { RawOptions } from '../../types/raw-options';

const version = 3;
export default {
    loadOptions() {
        const loaded = localStorage['sharplab.options'] || localStorage['tryroslyn.options'];
        if (!loaded)
            return null;

        try {
            const parsed = JSON.parse(loaded) as { version?: number };
            if (parsed.version !== version)
                return null;
            return (extendType(parsed)<{ options: RawOptions }>()).options;
        }
        catch (ex) {
            warn('Failed to load options:', ex);
            return null;
        }
    },

    saveOptions(options: RawOptions) {
        try {
            localStorage['sharplab.options'] = JSON.stringify({ version, options });
        }
        catch (ex) {
            warn('Failed to save options:', ex);
        }
    }
};
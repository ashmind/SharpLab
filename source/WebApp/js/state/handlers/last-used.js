import warn from '../../helpers/warn.js';
const version = 3;
export default {
    loadOptions: function() {
        const loaded = localStorage['sharplab.options'] || localStorage['tryroslyn.options'];
        if (!loaded)
            return null;

        try {
            const parsed = JSON.parse(loaded);
            if (parsed.version !== version)
                return null;
            return parsed.options;
        }
        catch (ex) {
            warn('Failed to load options:', ex);
            return null;
        }
    },

    saveOptions: function(options) {
        try {
            localStorage['sharplab.options'] = JSON.stringify({ version, options });
        }
        catch (ex) {
            warn('Failed to save options:', ex);
        }
    }
};
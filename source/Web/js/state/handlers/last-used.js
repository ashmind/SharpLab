import warn from 'helpers/warn';

export default {
    loadOptions: function() {
        var loaded = localStorage['tryroslyn.options'];
        if (!loaded)
            return null;

        try {
            return JSON.parse(loaded);
        }
        catch (ex) {
            warn('Failed to load options:', ex);
            return null;
        }
    },

    saveOptions: function(options) {
        try {
            localStorage['tryroslyn.options'] = JSON.stringify(options);
        }
        catch (ex) {
            warn('Failed to save options:', ex);
        }
    }
};
export default {
    loadOptions: function() {
        var loaded = localStorage['tryroslyn.options'];
        if (!loaded)
            return null;

        try {
            return JSON.parse(loaded);
        }
        catch(_) {
            return null;
        }
    },
            
    saveOptions: function(options) {
        localStorage['tryroslyn.options'] = JSON.stringify(options);
    }
};
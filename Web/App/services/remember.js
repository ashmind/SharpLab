angular.module('app').service('rememberService', [function() {
    'use strict';

    this.load = function() {
        var loaded = localStorage['tryroslyn.options'];
        if (!loaded)
            return null;

        loaded = JSON.parse(loaded);
        return loaded;
    };

    this.save = function(options) {
        localStorage['tryroslyn.options'] = JSON.stringify(options);
    };
}]);
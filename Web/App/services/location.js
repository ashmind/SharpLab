angular.module('app').service('$location', ['$window', '$rootScope', function ($window, $rootScope) {
    'use strict';
    
    // replaces standard angular location service
    // main purpose is to support simple hashes without
    // #/ or #!

    ['absUrl', 'url', 'protocol', 'host', 'port', 'path', 'search', 'replace'].forEach(function(name) {
        this[name] = function() {
            throw new Error("Not implemented: " + name + ".");
        }
    }, this);

    this.hash = function(value) {
        if (value !== undefined)
            $window.location.hash = value;

        return $window.location.hash;
    }

    $window.addEventListener('hashchange', function() {
        $rootScope.$emit('$locationChangeSuccess');
    }, false);
}]);
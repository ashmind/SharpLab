angular.module('app').factory('Modes', [function() {
    return Object.freeze({
        regular: 'regular',
        script:  'script'
    });
}]);
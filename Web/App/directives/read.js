angular.module('app').directive('appRead', function() {
    'use strict';

    function link(scope, $element, attrs) {
        scope[attrs.appRead] = $element.html();
    };

    return {
        restrict: 'A',
        link: link
    };
});
angular.module('app').directive('appBooleanModel', function() {
    'use strict';

    function link(scope, element, attr, ngModel) {
        ngModel.$parsers.push(function(string) {
            return string === 'true';
        });
        ngModel.$formatters.push(function(boolean) {
            return boolean ? 'true' : 'false';
        });
    };

    return {
        restrict: 'A',
        require: 'ngModel',
        link: link
    };
});
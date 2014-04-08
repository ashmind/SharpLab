angular.module('app').directive('appLoader', function () {
    'use strict';

    return {
        restrict: 'E',
        replace: true,
        templateUrl: 'loader/loader.html'
    };
});
angular.module('app').controller('FooterController', ['$scope', 'CompilationService', function ($scope, compilationService) {
    'use strict';

    compilationService.getRoslynVersion().then(function(value) {
        $scope.roslynVersion = value;
    });
}]);
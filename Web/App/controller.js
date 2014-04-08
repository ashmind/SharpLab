angular.module('app').controller('AppController', ['$scope', 'UrlService', 'CompilationService', function ($scope, urlService, compilationService) {
    'use strict';

    $scope.code = urlService.loadFromUrl();
    var unwatchDefault = $scope.$watch('defaultCode', function() {
        $scope.code = $scope.code || $scope.defaultCode;
        unwatchDefault();
    });
    compilationService.getRoslynVersion().then(function(value) {
        $scope.roslynVersion = value;
    });

    var saveToUrlThrottled = $.debounce(100, urlService.saveToUrl);
    var updateFromServerThrottled = $.debounce(600, updateFromServer);

    $scope.$watch('code', function(value) {
        saveToUrlThrottled(value);
        updateFromServerThrottled(value);
    });

    $scope.loading = false;
    function updateFromServer(code) {
        if (code == undefined || code === '')
            return;

        if ($scope.loading)
            return;

        $scope.loading = true;
        compilationService.process(code).then(function (data) {
            $scope.loading = false;
            $scope.result = data;
        });
    }
}]);
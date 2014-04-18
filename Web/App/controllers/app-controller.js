angular.module('app').controller('AppController', ['$scope', 'UrlService', 'CompilationService', function ($scope, urlService, compilationService) {
    'use strict';

    setupCode();
    $scope.toggleSyntaxTree = function() {
        $scope.syntaxTreeExpanded = !$scope.syntaxTreeExpanded;
    };

    function setupCode() {
        $scope.code = urlService.loadFromUrl();
        var unwatchDefault = $scope.$watch('defaultCode', function () {
            $scope.code = $scope.code || $scope.defaultCode;
            unwatchDefault();
        });

        var saveToUrlThrottled = $.debounce(100, urlService.saveToUrl);
        var updateFromServerThrottled = $.debounce(600, updateFromServer);
        $scope.$watch('code', function(value) {
            saveToUrlThrottled(value);
            updateFromServerThrottled(value);
        });
    }

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
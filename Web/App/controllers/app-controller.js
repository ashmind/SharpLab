angular.module('app').controller('AppController', ['$scope', 'UrlService', 'CompilationService', function ($scope, urlService, compilationService) {
    'use strict';

    $scope.branchName = null;
    compilationService.getBranchNames().then(function(value) {
        $scope.branchNames = value;
    });

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
            saveToUrlThrottled(value, $scope.branchName);
            updateFromServerThrottled(value, $scope.branchName);
        });

        $scope.$watch('branchName', function(value) {
            urlService.saveToUrl($scope.code, value);
            updateFromServer($scope.code, value);
        });
    }

    $scope.loading = false;
    function updateFromServer(code, branchName) {
        if (code == undefined || code === '')
            return;

        if ($scope.loading)
            return;

        $scope.loading = true;
        compilationService.process(code, branchName).then(function (data) {
            $scope.loading = false;
            $scope.result = data;
        });
    }
}]);
angular.module('app').controller('AppController', ['$scope', 'UrlService', 'CompilationService', function ($scope, urlService, compilationService) {
    'use strict';

    $scope.branchName = null;
    compilationService.getBranches().then(function(value) {
        $scope.branches = value.map(function(b) {
            return { name: b.name, text: b.name + " (" + moment(b.timestamp).format("DMMM") + ")" };
        });
    });

    setup();
    $scope.toggleSyntaxTree = function() {
        $scope.syntaxTreeExpanded = !$scope.syntaxTreeExpanded;
    };

    function setup() {
        var urlData = urlService.loadFromUrl();
        if (urlData) {
            $scope.code = urlData.code;
            $scope.branchName = urlData.branch;
        }

        var unwatchDefault = $scope.$watch('defaultCode', function () {
            $scope.code = $scope.code || $scope.defaultCode;
            unwatchDefault();
        });

        var saveScopeToUrlThrottled = $.debounce(100, saveScopeToUrl);
        var updateFromServerThrottled = $.debounce(600, updateFromServer);
        $scope.$watch('code', function() {
            saveScopeToUrlThrottled();
            updateFromServerThrottled();
        });

        $scope.$watch('branchName', function() {
            saveScopeToUrl();
            updateFromServer();
        });
    }

    function saveScopeToUrl() {
        urlService.saveToUrl({
            code: $scope.code,
            branch: $scope.branchName
        });
    }

    $scope.loading = false;
    function updateFromServer() {
        if ($scope.code == undefined || $scope.code === '')
            return;

        if ($scope.loading)
            return;

        $scope.loading = true;
        compilationService.process($scope.code, $scope.branchName).then(function (data) {
            $scope.loading = false;
            $scope.result = data;
        });
    }
}]);
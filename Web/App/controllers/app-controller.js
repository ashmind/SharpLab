angular.module('app').controller('AppController', ['$scope', '$filter', 'UrlService', 'CompilationService', function ($scope, $filter, urlService, compilationService) {
    'use strict';

    $scope.branch = null;
    var branchesPromise = compilationService.getBranches().then(function(value) {
        $scope.branches = value;
        $scope.branches.forEach(function(b) {
            b.lastCommitDate = new Date(b.lastCommitDate);
        });
    });
    $scope.displayBranch = function(branch) {
        return branch.name + " (" + $filter('date')(branch.lastCommitDate, "d MMM") + ")";
    };

    setup();
    $scope.expanded = {};
    $scope.expanded = function(name) {
        $scope.expanded[name] = true;
    }
    $scope.toggle = function(name) {
        $scope.expanded[name] = !$scope.expanded[name];
    };


    function setup() {
        var urlData = urlService.loadFromUrl();
        if (urlData) {
            $scope.code = urlData.code;
            branchesPromise.then(function() {
                $scope.branch = $scope.branches.filter(function(b) { return b.name === urlData.branch; })[0] || null;
            });
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

        $scope.$watch('branch', function() {
            saveScopeToUrl();
            updateFromServer();
        });
    }

    function saveScopeToUrl() {
        urlService.saveToUrl({
            code: $scope.code,
            branch: ($scope.branch || {}).name
        });
    }

    $scope.loading = false;
    function updateFromServer() {
        if ($scope.code == undefined || $scope.code === '')
            return;

        if ($scope.loading)
            return;

        $scope.loading = true;
        compilationService.process($scope.code, ($scope.branch || {}).name).then(function (data) {
            $scope.loading = false;
            $scope.result = data;
        }, function(response) {
            $scope.loading = false;
            $scope.result = {
                success: false,
                errors: [ response.data.message ]
            };
        });
    }
}]);
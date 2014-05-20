angular.module('app').controller('AppController', ['$scope', '$filter', 'UrlService', 'CompilationService', 'Modes', function ($scope, $filter, urlService, compilationService, modes) {
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
            $scope.mode = urlData.mode;
            $scope.optimizations = urlData.optimizations;
            branchesPromise.then(function() {
                $scope.branch = $scope.branches.filter(function(b) { return b.name === urlData.branch; })[0] || null;
            });
        }
        else {
            $scope.mode = modes.regular;
        }

        var unwatchDefault = $scope.$watch('defaultCode', function () {
            $scope.code = $scope.code || $scope.defaultCode;
            unwatchDefault();
        });

        var saveScopeToUrlThrottled = $.debounce(100, saveScopeToUrl);
        var updateFromServerThrottled = $.debounce(600, processOnServer);
        $scope.$watch('code', function() {
            saveScopeToUrlThrottled();
            updateFromServerThrottled();
        });

        var updateImmediate = function() {
            saveScopeToUrl();
            processOnServer();
        };
        $scope.$watch('branch', updateImmediate);
        $scope.$watch('mode', updateImmediate);
        $scope.$watch('optimizations', updateImmediate);
    }

    function saveScopeToUrl() {
        urlService.saveToUrl({
            code: $scope.code,
            mode: $scope.mode,
            optimizations: $scope.optimizations,
            branch: ($scope.branch || {}).name
        });
    }

    $scope.loading = false;
    function processOnServer() {
        if ($scope.code == undefined || $scope.code === '')
            return;

        if ($scope.loading)
            return;

        $scope.loading = true;
        compilationService.process($scope.code, $scope.mode, $scope.optimizations, ($scope.branch || {}).name).then(function (data) {
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
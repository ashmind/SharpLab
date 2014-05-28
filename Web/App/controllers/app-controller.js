angular.module('app').controller('AppController', ['$scope', '$filter', 'DefaultCodeService', 'UrlService', 'CompilationService', function ($scope, $filter, defaultCodeService, urlService, compilationService) {
    'use strict';

    $scope.languages = Object.freeze([
        { language: 'csharp', displayName: 'C#' },
        { language: 'vbnet',  displayName: 'VB.NET' }
    ]);

    $scope.codeMirrorModes = Object.freeze({
        csharp: 'text/x-csharp',
        vbnet:  'text/x-vb'
    });

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
            $scope.options = urlData.options;
            branchesPromise.then(function() {
                $scope.branch = $scope.branches.filter(function(b) { return b.name === urlData.branch; })[0] || null;
            });
        }
        
        $scope.options = angular.extend({
            language: 'csharp',
            target:   'csharp',
            mode:     'regular',
            optimizations: false
        }, $scope.options);
        defaultCodeService.attach($scope);

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
        for (var key in $scope.options) {
            if (key.indexOf('$') > -1)
                continue;

            $scope.$watch('options.' + key, updateImmediate);
        }
    }
    
    function saveScopeToUrl() {
        urlService.saveToUrl({
            code: $scope.code,
            options: $scope.options,
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
        compilationService.process($scope.code, $scope.options, ($scope.branch || {}).name).then(function (data) {
            $scope.loading = false;
            $scope.result = data;
        }, function(response) {
            $scope.loading = false;
            var error = response.data;
            var report = error.exceptionMessage || error.message;
            if (error.stackTrace)
                report += "\r\n" + error.stackTrace;

            $scope.result = {
                success: false,
                errors: [ report ]
            };
        });
    }
}]);
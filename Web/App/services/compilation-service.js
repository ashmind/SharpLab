angular.module('app').service('CompilationService', ['$http', function ($http) {
    this.getBranches = function() {
        return $http.get('api/branches').then(function(response) {
            return response.data;
        });
    }

    this.process = function (code, mode, optimizations, branchName) {
        var url = 'api/compilation';
        return $http({
            url:    url,
            method: 'POST',
            data: {
                code:          code,
                mode:          mode,
                optimizations: optimizations,
                branch:        branchName
            },
            headers: {
                'Content-Type': 'application/json',
                'Accepts':      'application/json'
            }
        }).then(function (response) {
            return response.data;
        });
    };
}]);
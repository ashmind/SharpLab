angular.module('app').service('CompilationService', ['$http', function ($http) {
    this.getRoslynVersion = function () {
        return $http.get('api/info').then(function(response) {
            return response.data.roslynVersion;
        });
    };

    this.getBranchNames = function() {
        return $http.get('api/branches').then(function(response) {
            return response.data;
        });
    }

    this.process = function (code, branchName) {
        var url = 'api/compilation';
        if (branchName)
            url += '?branch=' + branchName;

        return $http({
            url:    url,
            method: 'POST',
            data:   code,
            headers: {
                'Content-Type': 'text/x-csharp',
                'Accepts':      'application/json'
            }
        }).then(function (response) {
            return response.data;
        });
    };
}]);
angular.module('app').service('CompilationService', ['$http', function ($http) {
    this.getRoslynVersion = function () {
        return $http.get('api/info').then(function(response) {
            return response.data.roslynVersion;
        });
    };

    this.process = function (code) {
        return $http({
            url:    'api/compilation',
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
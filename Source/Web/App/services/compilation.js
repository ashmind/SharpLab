angular.module('app').service('compilationService', ['$http', function ($http) {
    'use strict';

    this.getBranches = function() {
        return $http.get('api/branches').then(function(response) {
            return response.data;
        });
    };

    this.process = function (code, options, branchName) {
        var url = 'api/compilation';
        var data = {
            code:   code,
            branch: branchName
        };
        angular.extend(data, options);

        return $http({
            url:    url,
            method: 'POST',
            data:   data,
            headers: {
                'Content-Type': 'application/json',
                'Accepts':      'application/json'
            }
        }).then(function (response) {
            return response.data;
        });
    };
}]);
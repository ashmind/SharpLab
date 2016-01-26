angular.module('app').service('compilationService', ['$http', function ($http) {
    'use strict';

    this.process = function (code, options, branchUrl) {
        var url = 'api/compilation';
        if (branchUrl)
            url = branchUrl.replace(/\/?$/, '/') + url;

        var data = { code: code };
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
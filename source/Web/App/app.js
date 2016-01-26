var app = angular.module('app', []);
app.filter('trim', [function() {
    return function(value) {
        return value.trim();
    }
}]);
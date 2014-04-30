angular.module('app', []).config(function ($locationProvider) {
    $locationProvider.html5Mode(true).hashPrefix('');
});
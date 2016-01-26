angular.module('app').directive('appMobileShelf', ['$window', function($window) {
    'use strict';
    // ReSharper disable once InconsistentNaming
    var Hammer = $window.Hammer;

    function link($scope, $element, attrs) {
        var options = $scope.$eval(attrs.appMobileShelf);
        var $container = options.container ? $(options.container) : null;
        var $classChangeTarget = $container || $element;

        $(options.toggle).click(function() {
            $classChangeTarget.toggleClass(options.openClass);
        });

        if ($container) {
            Hammer($container[0])
                .on('swipeleft', function() {
                    $container.removeClass(options.openClass);
                })
                .on('swiperight', function() {
                    $container.addClass(options.openClass);
                });
        }
    };

    return {
        restrict: 'A',
        link: link
    };
}]);
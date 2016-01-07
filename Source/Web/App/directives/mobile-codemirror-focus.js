angular.module('app').directive('appMobileCodemirrorFocus', [function () {
    'use strict';

    function link($scope, $element, attrs) {
        var className = attrs.appMobileCodemirrorFocus;
        $element.find('.CodeMirror').each(function() {
            this.CodeMirror.on('focus', function() {
                $element.addClass(className);
            });
            this.CodeMirror.on('blur', function () {
                $element.removeClass(className);
            });
        });
    };

    return {
        restrict: 'A',
        link: link
    };
}]);
angular.module('app').directive('appCleanread', function() {
    'use strict';

    function link(scope, $element, attrs) {
        scope[attrs.appCleanread] = cleanIndents($element.text());
    };

    function cleanIndents(text) {
        text = text.trim();

        var lines = text.trim().split(/[\r\n]+/g);
        var indent = lines[lines.length - 1].match(/^\s*/)[0];
        return text.replace(new RegExp(indent, 'g'), '');
    }

    return {
        restrict: 'A',
        link: link
    };
});
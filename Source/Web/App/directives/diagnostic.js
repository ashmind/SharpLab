angular.module('app').directive('appDiagnostic', function() {
    'use strict';
    return {
        restrict: 'E',
        replace: true,
        scope: { diagnostic: '=bind' },
        template: '<div>' +
            '({{diagnostic.start.line}},{{diagnostic.start.column}},{{diagnostic.end.line}},{{diagnostic.end.column}}): ' +
            '{{diagnostic.severity}} {{diagnostic.id}}: {{diagnostic.message}}' +
        '</div>'
    };
});
angular.module('app').directive('appCodemirror', function() {
    'use strict';

    function link(scope, $element, attrs) {
        var instance = CodeMirror.fromTextArea($element[0], {
            mode:        scope.mode,
            lineNumbers: attrs.linenumbers != undefined,
            readOnly:    attrs.readonly != undefined,
            indentUnit:  4
        });

        scope.$watch('mode', function(value) {
            instance.setOption("mode", value);
        });

        var settingValue = false;
        scope.$watch('value', function (value) {
            value = value != undefined ? value : '';
            if (instance.getValue() === value)
                return;

            settingValue = true;
            instance.setValue(value);
            settingValue = false;
        });

        instance.on('change', function () {
            if (settingValue)
                return;

            var value = instance.getValue();
            if (value === scope.value)
                return;
            
            scope.$apply(function() {
                scope.value = value;
            });
        });
    };

    return {
        restrict: 'E',
        replace: true,
        template: '<textarea></textarea>',
        scope: {
            value: '=value',
            mode:  '=mode'
        },
        link: link
    };
});
angular.module('app').directive('appSyntaxtree', function() {
    'use strict';

    function link($scope, $element, attrs) {
        if ($scope.root)
            renderSubtree($element, $scope.root);

        $scope.$watch('root', function (newRoot) {
            $element.empty();
            renderSubtree($element, newRoot);
        });
    };

    function renderSubtree($ol, root) {
        var $li = $('<li><span>' + root.kind + '</span></li>');
        if (root.nodes.length > 0) {
            $li.addClass('with-children');

            var $childOl = $('<ol>').appendTo($li);
            root.nodes.forEach(function(node) {
                renderSubtree($childOl, node);
            });
        }

        $ol.append($li);
    }

    return {
        restrict: 'E',
        replace: true,
        template: '<ol></ol>',
        scope: { root: '=root' },
        link: link
    };
});
angular.module('app').service('DefaultCodeService', ['$q', function ($q) {
    var defaults = {
        csharp: 'using System;\r\npublic class C(int x) {\r\n    public void M() {\r\n    }\r\n\}',
        vbnet:  'Imports System\r\nPublic Class C\r\n\    Public Sub M()\r\n    End Sub\r\nEnd Class'
    };

    this.attach = function($scope) {
        if (!$scope.code)
            setDefaultCode($scope);

        $scope.$watch('options.language', function() {
            if ($scope.code !== $scope.defaultCode)
                return;

            setDefaultCode($scope);
        });
    };

    function setDefaultCode($scope) {
        $scope.defaultCode = defaults[$scope.options.language];
        $scope.code = $scope.defaultCode;
    }
}]);
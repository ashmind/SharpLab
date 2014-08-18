angular.module('app').service('defaultsService', [function() {
    'use strict';

    var codes = {
        csharp: 'using System;\r\npublic class C(int x) {\r\n    public void M() {\r\n    }\r\n}',
        vbnet:  'Imports System\r\nPublic Class C\r\n    Public Sub M()\r\n    End Sub\r\nEnd Class'
    };

    this.getOptions = function() {
        return {
            branch:        null,
            language:      'csharp',
            target:        'csharp',
            mode:          'regular',
            optimizations: false
        };
    };

    this.getCode = function(language) {
        return codes[language];
    };
}]);
const code = {
    csharp: 'using System;\r\nusing System.Collections.Generic;\r\nusing System.Linq;\r\nusing System.Runtime.CompilerServices;\r\nusing System.Threading.Tasks;\r\n\r\npublic class Class1\r\n{\r\n    public void Method1()\r\n    {\r\n    }\r\n}',
    vbnet:  'Imports System\r\nImports System.Collections.Generic\r\nImports System.Linq\r\nImports System.Runtime.CompilerServices\r\nImports System.Threading.Tasks\r\n\r\nPublic Class Class1\r\n    Public Sub Method1()\r\n    End Sub\r\nEnd Class'
};

export default {
    getOptions: () => ({
        branch:        null,
        language:      'csharp',
        target:        'csharp',
        mode:          'regular',
        optimizations: false
    }),
    
    getCode: (language) => code[language]
};

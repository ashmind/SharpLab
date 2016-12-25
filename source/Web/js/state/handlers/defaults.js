const code = {
    csharp: 'using System;\r\npublic class C {\r\n    public void M() {\r\n    }\r\n}',
    vbnet:  'Imports System\r\nPublic Class C\r\n    Public Sub M()\r\n    End Sub\r\nEnd Class'
};

export default {
    getOptions: () => ({
        branch:     null,
        language:   'csharp',
        target:     'csharp',
        mode:       'regular',
        release:    false
    }),
    
    getCode: (language) => code[language]
};
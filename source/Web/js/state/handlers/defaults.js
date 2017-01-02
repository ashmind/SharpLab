import languages from 'helpers/languages';

const code = {
    [languages.csharp]: 'using System;\r\npublic class C {\r\n    public void M() {\r\n    }\r\n}',
    [languages.vb]:  'Imports System\r\nPublic Class C\r\n    Public Sub M()\r\n    End Sub\r\nEnd Class'
};

export default {
    getOptions: () => ({
        branch:     null,
        language:   languages.csharp,
        target:     languages.csharp,
        mode:       'regular',
        release:    false
    }),
    
    getCode: (language) => code[language]
};
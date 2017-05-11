import languages from '../../helpers/languages.js';

const code = {
    [languages.csharp]: 'using System;\r\n\r\npublic class C\r\n{\r\n    public void M()\r\n{\r\n    }\r\n}',
    [languages.vb]:  'Imports System\r\nPublic Class C\r\n    Public Sub M()\r\n    End Sub\r\nEnd Class'
};

export default {
    getOptions: () => ({
        branchId:   null,
        language:   languages.csharp,
        target:     languages.csharp,
        release:    false
    }),

    getCode: (language) => code[language]
};

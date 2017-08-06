import languages from '../../helpers/languages.js';
import targets from '../../helpers/targets.js';

const code = {
    [languages.csharp]: 'using System;\r\npublic class C {\r\n    public void M() {\r\n    }\r\n}',
    [languages.vb]: 'Imports System\r\nPublic Class C\r\n    Public Sub M()\r\n    End Sub\r\nEnd Class',
    [languages.fsharp]: 'open System\r\ntype C() =\r\n    member this.M() = ()',

    [`${languages.csharp}.run`]: 'using System;\r\npublic static class Program {\r\n    public static void Main() {\r\n        Console.WriteLine("🌄");\r\n    }\r\n}',
    [`${languages.vb}.run`]: 'Imports System\r\nPublic Module Program\r\n    Public Sub Main()\r\n        Console.WriteLine("🌄")\r\n    End Sub\r\nEnd Module',
    [`${languages.fsharp}.run`]: 'printfn "🌄"',
};

export default {
    getOptions: () => ({
        branchId:   null,
        language:   languages.csharp,
        target:     languages.csharp,
        release:    false
    }),

    getCode: (language, target) => code[target === targets.run ? language + '.run' : language]
};
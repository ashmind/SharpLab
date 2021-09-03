import { languages, LanguageName } from '../../helpers/languages';
import { targets, TargetName } from '../../helpers/targets';
import help from '../../helpers/help';
import asLookup from '../../helpers/as-lookup';

const code = asLookup({
    [languages.csharp]: 'using System;\r\npublic class C {\r\n    public void M() {\r\n    }\r\n}',
    [languages.vb]: 'Imports System\r\nPublic Class C\r\n    Public Sub M()\r\n    End Sub\r\nEnd Class',
    [languages.fsharp]: 'open System\r\ntype C() =\r\n    member _.M() = ()',
    [languages.IL]: '.assembly ConsoleApp\r\n{\r\n}\r\n\r\n.class private auto ansi \'<Module>\'\r\n{\r\n}\r\n\r\n.class public auto ansi beforefieldinit C\r\nextends [System.Private.CoreLib]System.Object\r\n{\r\n   .method public hidebysig \r\n        instance void M () cil managed \r\n    {\r\n       .maxstack 8\r\n        ret\r\n    }\r\n\r\n   .method public hidebysig specialname rtspecialname \r\n        instance void .ctor () cil managed \r\n   {\r\n        .maxstack 8\r\n        ldarg.0\r\n        call instance void [System.Private.CoreLib]System.Object::.ctor()\r\n        ret\r\n    }\r\n}',

    [`${languages.csharp}.run`]: `${help.run.csharp}\r\nusing System;\r\n\r\nConsole.WriteLine("🌄");`,
    [`${languages.vb}.run`]: 'Imports System\r\nPublic Module Program\r\n    Public Sub Main()\r\n        Console.WriteLine("🌄")\r\n    End Sub\r\nEnd Module',
    [`${languages.fsharp}.run`]: 'printfn "🌄"',
    [`${languages.IL}.run`]: '.assembly ConsoleApp\r\n{\r\n}\r\n\r\n.class private auto ansi \'<Module>\'\r\n{\r\n}\r\n\r\n.class public auto ansi beforefieldinit C\r\nextends [System.Private.CoreLib]System.Object\r\n{\r\n   .method public hidebysig \r\n        static void Main () cil managed \r\n    {\r\n       .maxstack 8\r\n       ldstr "Hello IL!"\r\n       call void [System.Console]System.Console::WriteLine(string)\r\n        ret\r\n    }\r\n\r\n   .method public hidebysig specialname rtspecialname \r\n        instance void .ctor () cil managed \r\n   {\r\n        .maxstack 8\r\n        ldarg.0\r\n        call instance void [System.Private.CoreLib]System.Object::.ctor()\r\n        ret\r\n    }\r\n}'
} as const);

export default {
    getOptions: () => ({
        language:   languages.csharp,
        target:     languages.csharp,
        release:    false
    }),

    getCode: (language: LanguageName|undefined, target: TargetName|string|undefined) => code[
        (target === targets.run ? language + '.run' : language) as string
    ] ?? ''
};
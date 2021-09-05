import { languages, LanguageName } from '../../helpers/languages';
import { targets, TargetName } from '../../helpers/targets';
import help from '../../helpers/help';
import asLookup from '../../helpers/as-lookup';

const normalize = (code: string) => {
    // 8 spaces must match the layout below
    return code
        .replace(/^ {8}/gm, '')
        .replace(/(\r\n|\r|\n)/g, '\r\n')
        .trim();
};

const code = asLookup({
    [languages.csharp]: 'using System;\r\npublic class C {\r\n    public void M() {\r\n    }\r\n}',
    [languages.vb]: 'Imports System\r\nPublic Class C\r\n    Public Sub M()\r\n    End Sub\r\nEnd Class',
    [languages.fsharp]: 'open System\r\ntype C() =\r\n    member _.M() = ()',
    [languages.il]: normalize(`
        .assembly A
        {
        }

        .class public auto ansi abstract sealed beforefieldinit C
            extends [System.Private.CoreLib]System.Object
        {
            .method public hidebysig static
                void M () cil managed
            {
                .maxstack 8

                ret
            }
        }
    `),

    [`${languages.csharp}.run`]: `${help.run.csharp}\r\nusing System;\r\n\r\nConsole.WriteLine("🌄");`,
    [`${languages.vb}.run`]: 'Imports System\r\nPublic Module Program\r\n    Public Sub Main()\r\n        Console.WriteLine("🌄")\r\n    End Sub\r\nEnd Module',
    [`${languages.fsharp}.run`]: 'printfn "🌄"',
    [`${languages.il}.run`]: normalize(`
        .assembly ConsoleApp
        {
        }

        .class public auto ansi abstract sealed beforefieldinit Program
            extends [System.Private.CoreLib]System.Object
        {
            .method public hidebysig static
                void Main () cil managed
            {
                .entrypoint
                .maxstack 8

                ldstr "🌄"
                call void [System.Console]System.Console::WriteLine(string)
                ret
            }
        }
    `)
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
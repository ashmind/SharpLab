import { targets, TargetName } from '../../helpers/targets';
import help from '../../helpers/help';
import asLookup from '../../helpers/as-lookup';
import { LANGUAGE_CSHARP, LANGUAGE_FSHARP, LANGUAGE_IL, LANGUAGE_VB, LanguageName } from '../../../app/shared/languages';

const normalize = (code: string) => {
    // 8 spaces must match the layout below
    return code
        .replace(/^ {8}/gm, '')
        .replace(/(\r\n|\r|\n)/g, '\r\n')
        .trim();
};

const code = asLookup({
    [LANGUAGE_CSHARP]: 'using System;\r\npublic class C {\r\n    public void M() {\r\n    }\r\n}',
    [LANGUAGE_VB]: 'Imports System\r\nPublic Class C\r\n    Public Sub M()\r\n    End Sub\r\nEnd Class',
    [LANGUAGE_FSHARP]: 'open System\r\ntype C() =\r\n    member _.M() = ()',
    [LANGUAGE_IL]: normalize(`
        .assembly A
        {
        }

        .class public auto ansi abstract sealed beforefieldinit C
            extends System.Object
        {
            .method public hidebysig static
                void M () cil managed
            {
                .maxstack 8

                ret
            }
        }
    `),

    [`${LANGUAGE_CSHARP}.run`]: `${help.run.csharp}\r\nusing System;\r\n\r\nConsole.WriteLine("🌄");`,
    [`${LANGUAGE_VB}.run`]: 'Imports System\r\nPublic Module Program\r\n    Public Sub Main()\r\n        Console.WriteLine("🌄")\r\n    End Sub\r\nEnd Module',
    [`${LANGUAGE_FSHARP}.run`]: 'printfn "🌄"',
    [`${LANGUAGE_IL}.run`]: normalize(`
        .assembly ConsoleApp
        {
        }

        .class public auto ansi abstract sealed beforefieldinit Program
            extends System.Object
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
        language:   LANGUAGE_CSHARP,
        target:     LANGUAGE_CSHARP,
        release:    false
    } as const),

    getCode: (language: LanguageName|undefined, target: TargetName|string|undefined) => code[
        (target === targets.run ? language + '.run' : language) as string
    ] ?? ''
};
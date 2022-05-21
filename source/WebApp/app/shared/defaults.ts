import { asLookup } from '../helpers/asLookup';
import { CSHARP_RUN_HELP } from './help';
import { LANGUAGE_CSHARP, LANGUAGE_VB, LANGUAGE_FSHARP, LANGUAGE_IL, type LanguageName } from './languages';
import { type TargetName, TARGET_RUN } from './targets';

const normalize = (code: string) => {
    // 8 spaces must match the layout below
    return code
        .replace(/^ {8}/gm, '')
        .replace(/(\r\n|\r|\n)/g, '\r\n')
        .trim();
};

const defaultCode = asLookup({
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

    [`${LANGUAGE_CSHARP}.run`]: `${CSHARP_RUN_HELP}\r\nusing System;\r\n\r\nConsole.WriteLine("🌄");`,
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

export const DEFAULT_OPTIONS = ({
    language:   LANGUAGE_CSHARP,
    target:     LANGUAGE_CSHARP,
    release:    false
} as const);

export const getDefaultCode = (language: LanguageName|undefined, target: TargetName|string|undefined) => defaultCode[
    (target === TARGET_RUN ? language + '.run' : language) as string
] ?? '';
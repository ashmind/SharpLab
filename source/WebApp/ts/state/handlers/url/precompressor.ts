import help from '../../../helpers/help';
import { LANGUAGE_CSHARP, LanguageName, LANGUAGE_IL } from '../../../../app/shared/languages';
import { asLookup } from '../../../../app/helpers/asLookup';

const dictionaries = asLookup({
    [LANGUAGE_CSHARP]: build(
        'using',
        'System',
        'class',
        'public',
        'void',
        'Func',
        'Task',
        'return',
        'async',
        'await',
        'string',
        'yield',
        'Action',
        'IEnumerable',
        'System.Collections.Generic',
        'System.Threading.Tasks',
        'static',
        'Program',
        'Main',
        'Console.WriteLine',
        help.run.csharp,
        'using System;',
        'public static void Main()',
        'public static class Program',
        'Inspect.Allocations(() =>',
        'Inspect.MemoryGraph('
    ),

    [LANGUAGE_IL]: build(
        'Main ()',
        'Program',
        'ConsoleApp',
        'cil managed',
        '.entrypoint',
        '.maxstack',
        '.assembly',
        '.class public auto ansi abstract sealed beforefieldinit',
        'extends System.Object',
        '.method public hidebysig',
        'call void [System.Console]System.Console::WriteLine('
    )
});

function build(...entries: ReadonlyArray<string>) {
    const sortedByLength = entries.slice(0);
    sortedByLength.sort((a, b) => Math.sign(b.length - a.length));

    const pattern = String.raw`@|(?:${sortedByLength.map(escapeRegex).join('|')}')(?=[^\d]|$)`;
    return { entries, regex: new RegExp(pattern, 'mg') };
}

function compress(code: string, language: LanguageName) {
    const dictionary = dictionaries[language];
    if (!dictionary)
        return code.replace('@', '@@');
    return code.replace(dictionary.regex, m => {
        if (m === '@')
            return '@@';
        return '@' + dictionary.entries.indexOf(m); // eslint-disable-line prefer-template
    });
}

function decompress(compressed: string, language: LanguageName) {
    const dictionary = dictionaries[language];
    if (!dictionary)
        return compressed.replace('@@', '@');
    return compressed.replace(/@(\d+|@)/g, (m, $1) => {
        if (m === '@@')
            return '@';
        return dictionary.entries[parseInt($1, 10)];
    });
}

function escapeRegex(value: string) {
    return value.replace(/[-\\^$*+?.()|[\]{}]/g, '\\$&');
}

export default {
    compress,
    decompress
};
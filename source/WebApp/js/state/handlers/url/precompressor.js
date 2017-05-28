import languages from '../../../helpers/languages.js';

const dictionaries = {
    [languages.csharp]: build(
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
        'System.Threading.Tasks'
    )
};

function build(...entries) {
    const sortedByLength = entries.slice(0);
    sortedByLength.sort((a, b) => Math.sign(b.length - a.length));

    const pattern = '@|' + sortedByLength.map(e => e.replace('.', '\\.')).join('|'); // eslint-disable-line prefer-template
    return { entries, regex: new RegExp(pattern, 'g') };
}

function compress(code, language) {
    const dictionary = dictionaries[language];
    if (!dictionary)
        return code.replace('@', '@@');
    return code.replace(dictionary.regex, m => {
        if (m === '@')
            return '@@';
        return '@' + dictionary.entries.indexOf(m); // eslint-disable-line prefer-template
    });
}

function decompress(compressed, language) {
    const dictionary = dictionaries[language];
    if (!dictionary)
        return compressed.replace('@@', '@');
    return compressed.replace(/@(\d+|@)/g, (m, $1) => {
        if (m === '@@')
            return '@';
        return dictionary.entries[parseInt($1, 10)];
    });
}

export default {
    compress,
    decompress
};
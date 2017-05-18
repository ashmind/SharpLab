import languages from '../../helpers/languages.js';
import LZString from 'lz-string';

let lastHash;
function save(code, options) {
    let hash = LZString.compressToBase64(code);
    const flags = stringifyFlags(options);
    if (flags)
        hash = `f:${flags}/${hash}`;

    if (options.branchId)
        hash = `b:${options.branchId}/${hash}`;

    lastHash = hash;
    window.location.hash = hash;
}

function loadInternal(onlyIfChanged) {
    let hash = window.location.hash;
    if (!hash)
        return null;

    hash = decodeURIComponent(hash.replace(/^#/, ''));
    if (!hash || (onlyIfChanged && hash === lastHash))
        return null;

    lastHash = hash;
    const match = /(?:b:([^/]+)\/)?(?:f:([^/]+)\/)?(.+)/.exec(hash);
    if (match === null)
        return null;

    const result = {
        options: Object.assign({ branchId: match[1] }, parseFlags(match[2]))
    };

    try {
        result.code = LZString.decompressFromBase64(match[3]);
    }
    catch (e) {
        return null;
    }

    return result;
}

function load() {
    return loadInternal(false);
}

function onchange(callback) {
    window.addEventListener('hashchange', () => {
        const loaded = loadInternal(true);
        if (loaded !== null)
            callback(loaded);
    });
}

function reverseMap(map) {
    const result = {};
    for (const key in map) {
        result[map[key]] = key;
    }
    return result;
}

const targetMap = {
    [languages.csharp]: '',
    [languages.vb]:     '>vb',
    [languages.il]:     '>il',
    [languages.asm]:    '>asm'
};
const targetMapReverse = reverseMap(targetMap);

const languageMap = {
    [languages.csharp]: '',
    [languages.vb]:     'vb',
    [languages.fsharp]: 'fs'
};
const languageMapReverse = reverseMap(languageMap);

function stringifyFlags(options) {
    return [
        languageMap[options.language],
        targetMap[options.target],
        options.release ? 'r' : ''
    ].join('');
}

function parseFlags(flags) {
    if (!flags)
        return {};

    const match = flags.match(/^([^>]*?)(>.+?)?(r)?$/);
    if (!match)
        return {};

    return {
        language: languageMapReverse[match[1] || ''],
        target:   targetMapReverse[match[2] || ''],
        release:  match[3] === 'r'
    };
}

export default {
    save,
    load,
    onchange
};
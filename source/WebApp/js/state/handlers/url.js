import languages from '../../helpers/languages.js';
import LZString from 'lz-string';

let lastHash;
function save(code, options) {
    let hash = LZString.compressToBase64(code);
    const flags = stringifyFlags(options);
    if (flags)
        hash = 'f:' + flags + '/' + hash;

    if (options.branchId)
        hash = 'b:' + options.branchId + '/' + hash;

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
    const match = /(?:b:([^\/]+)\/)?(?:f:([^\/]+)\/)?(.+)/.exec(hash);
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
    window.addEventListener("hashchange", () => {
        const loaded = loadInternal(true);
        if (loaded !== null)
            callback(loaded);
    });
}

const targetMap = {
    [languages.csharp]: '',
    [languages.vb]:  '>vb',
    [languages.il]:  '>il',
    [languages.asm]: '>asm'
};
const targetMapReverse = (() => {
    const result = {};
    for (let key in targetMap) {
        result[targetMap[key]] = key;
    }
    return result;
})();

function stringifyFlags(options) {
    return [
        options.language === languages.vb ? 'vb' : '',
        targetMap[options.target],
        options.release ? 'r' : ''
    ].join('');
}

function parseFlags(flags) {
    if (!flags)
        return {};

    let target = targetMapReverse[''];
    for (var key in targetMapReverse) {
        if (key === '')
            continue;

        if (flags.indexOf(key) > -1)
            target = targetMapReverse[key];
    }

    return {
        language: /(^|[a-z])vb/.test(flags) ? languages.vb : languages.csharp,
        target:   target,
        release:  flags.indexOf('r') > -1
    };
}

export default {
    save,
    load,
    onchange
};
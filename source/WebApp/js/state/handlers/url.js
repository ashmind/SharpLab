import languages from '../../helpers/languages.js';
import targets from '../../helpers/targets.js';
import mapObject from '../../helpers/map-object.js';
import warn from '../../helpers/warn.js';
import precompressor from './url/precompressor.js';
import LZString from 'lz-string';

const languageAndTargetMap = {
    [languages.csharp]: '',
    [languages.vb]:     'vb',
    [languages.fsharp]: 'fs',
    [targets.il]:       'il',
    [targets.asm]:      'asm',
    [targets.ast]:      'ast'
};
const languageAndTargetMapReverse = mapObject(languageAndTargetMap, (key, value) => [value, key]);
const targetMapReverseV1 = mapObject(languageAndTargetMapReverse, (key, value) => [key !== '' ? '>' + key : '', value]); // eslint-disable-line prefer-template

let lastHash;
function save(code, options) {
    if (code == null) // too early?
        return;

    const optionsPacked = {
        b: options.branchId,
        l: languageAndTargetMap[options.language],
        t: languageAndTargetMap[options.target],
        d: options.release ? '' : '+'
    };
    const optionsPackedString = Object
        .entries(optionsPacked)
        .filter(([,value]) => !!value)
        .map(([key, value]) => key + ':' + value) // eslint-disable-line prefer-template
        .join(',');
    const precompressedCode = precompressor.compress(code, options.language);
    const hash = 'v2:' + LZString.compressToBase64(optionsPackedString + '|' + precompressedCode); // eslint-disable-line prefer-template

    lastHash = hash;
    window.location.hash = hash;
}

function load() {
    return loadInternal(false);
}

function loadInternal(onlyIfChanged) {
    let hash = window.location.hash;
    if (!hash)
        return null;

    hash = decodeURIComponent(hash.replace(/^#/, ''));
    if (!hash || (onlyIfChanged && hash === lastHash))
        return null;

    lastHash = hash;
    if (!hash.startsWith('v2:'))
        return legacyLoadFrom(hash);

    hash = hash.substring('v2:'.length);
    try {
        const parts = LZString.decompressFromBase64(hash).split('|', 2);
        const optionsPacked = parts[0].split(',').reduce((result, p) => {
            const [key, value] = p.split(':', 2);
            result[key] = value;
            return result;
        }, {});
        const language = languageAndTargetMapReverse[optionsPacked.l || ''];
        return {
            options: {
                branchId: optionsPacked.b,
                language,
                target:   languageAndTargetMapReverse[optionsPacked.t || ''],
                release:  optionsPacked.d !== '+'
            },
            code: precompressor.decompress(parts[1], language)
        };
    }
    catch (e) {
        warn('Failed to load state from URL:', e);
        return null;
    }
}

function onchange(callback) {
    window.addEventListener('hashchange', () => {
        const loaded = loadInternal(true);
        if (loaded !== null)
            callback(loaded);
    });
}

function legacyLoadFrom(hash) {
    const match = /(?:b:([^/]+)\/)?(?:f:([^/]+)\/)?(.+)/.exec(hash);
    if (match === null)
        return null;

    const flags = (match[2] || '').match(/^([^>]*?)(>.+?)?(r)?$/) || [];
    const result = {
        options: {
            branchId: match[1],
            language: languageAndTargetMapReverse[flags[1] || ''],
            target: targetMapReverseV1[flags[2] || ''],
            release: flags[3] === 'r'
        }
    };

    try {
        result.code = LZString.decompressFromBase64(match[3]);
    }
    catch (e) {
        result.code = '';
    }

    return result;
}

export default {
    save,
    load,
    onchange
};
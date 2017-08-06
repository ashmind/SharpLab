import languages from '../../helpers/languages.js';
import targets from '../../helpers/targets.js';
import mapObject from '../../helpers/map-object.js';
import warn from '../../helpers/warn.js';
import precompressor from './url/precompressor.js';
import getGistAsync from './url/get-gist-async.js';
import LZString from 'lz-string';

const languageAndTargetMap = {
    [languages.csharp]: 'cs',
    [languages.vb]:     'vb',
    [languages.fsharp]: 'fs',
    [targets.il]:       'il',
    [targets.asm]:      'asm',
    [targets.ast]:      'ast',
    [targets.run]:      'run'
};
const languageAndTargetMapReverse = mapObject(languageAndTargetMap, (key, value) => [value, key]);
const targetMapReverseV1 = mapObject(languageAndTargetMapReverse, (key, value) => ['>' + key, value]); // eslint-disable-line prefer-template

const last = {
    hash: null
};
function save(code, options) {
    if (code == null) // too early?
        return;

    if (last.gist && saveGist(code, options))
        return;

    const optionsPacked = {
        b: options.branchId,
        l: options.language !== languages.csharp ? languageAndTargetMap[options.language] : null,
        t: options.target !== targets.csharp ? languageAndTargetMap[options.target] : null,
        d: options.release ? '' : '+'
    };
    const optionsPackedString = Object
        .entries(optionsPacked)
        .filter(([,value]) => !!value)
        .map(([key, value]) => key + ':' + value) // eslint-disable-line prefer-template
        .join(',');
    const precompressedCode = precompressor.compress(code, options.language);
    const hash = 'v2:' + LZString.compressToBase64(optionsPackedString + '|' + precompressedCode); // eslint-disable-line prefer-template

    saveHash(hash);
}

function saveGist(code, options) {
    if (code !== last.gist.code || options.language !== last.gist.language)
        return false;
    let hash = 'gist:' + last.gist.id;
    const target = options.target !== getGistDefaultTarget(options.language)
        ? (languageAndTargetMap[options.target] || '')
        : '';
    if (target || options.branchId || !options.release)
        hash += '/' + target;
    if (options.branchId || !options.release)
        hash += '/' + (options.branchId || '');
    if (!options.release)
        hash += '/debug';
    saveHash(hash);
    return true;
}

function saveHash(hash) {
    last.hash = hash;
    window.location.hash = hash;
}

function loadAsync() {
    let hash = getCurrentHash();
    if (!hash)
        return null;

    last.hash = hash;
    if (hash.startsWith('gist:'))
        return loadFromGistAsync(hash);

    if (!hash.startsWith('v2:'))
        return legacyLoadFrom(hash);

    hash = hash.substring('v2:'.length);
    try {
        const decompressed = LZString.decompressFromBase64(hash);
        const [, optionsPart, codePart] = /^([^|]*)\|([\s\S]*)$/.exec(decompressed);
        const optionsPacked = optionsPart.split(',').reduce((result, p) => {
            const [key, value] = p.split(':', 2);
            result[key] = value;
            return result;
        }, {});
        const language = languageAndTargetMapReverse[optionsPacked.l || 'cs'];
        const code = precompressor.decompress(codePart, language);
        return {
            options: {
                branchId: optionsPacked.b,
                language,
                target:   languageAndTargetMapReverse[optionsPacked.t || 'cs'],
                release:  optionsPacked.d !== '+'
            },
            code
        };
    }
    catch (e) {
        warn('Failed to load state from URL:', e);
        return null;
    }
}

function changed(callback) {
    window.addEventListener('hashchange', () => {
        const hash = getCurrentHash();
        if (hash !== last.hash)
            callback();
    });
}

function getCurrentHash() {
    const hash = window.location.hash;
    if (!hash)
        return null;
    return decodeURIComponent(hash.replace(/^#/, ''));
}

async function loadFromGistAsync(hash) {
    const parts = hash.replace(/^gist:/, '').split('/');
    const gist = await getGistAsync(parts[0]);
    let target = getGistDefaultTarget(gist.language);
    if (parts[1] != null)
        target = languageAndTargetMapReverse[parts[1]];

    last.gist = gist;
    return {
        options: {
            branchId: parts[2],
            language: gist.language,
            target,
            release:  parts[3] !== 'debug'
        },
        code: gist.code
    };
}

function getGistDefaultTarget(language) {
    return language !== languages.fsharp ? language : languages.csharp;
}

function legacyLoadFrom(hash) {
    const match = /(?:b:([^/]+)\/)?(?:f:([^/]+)\/)?(.+)/.exec(hash);
    if (match === null)
        return null;

    const flags = (match[2] || '').match(/^([^>]*?)(>.+?)?(r)?$/) || [];
    const result = {
        options: {
            branchId: match[1],
            language: languageAndTargetMapReverse[flags[1] || 'cs'],
            target: targetMapReverseV1[flags[2] || '>cs'],
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
    loadAsync,
    changed
};
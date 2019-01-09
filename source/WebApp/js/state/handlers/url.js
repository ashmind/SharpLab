import LZString from 'lz-string';
import languages from '../../helpers/languages.js';
import targets from '../../helpers/targets.js';
import warn from '../../helpers/warn.js';
import {
    languageAndTargetMap,
    languageAndTargetMapReverse,
    targetMapReverseV1
} from './helpers/language-and-target-maps.js';
import precompressor from './url/precompressor.js';
import loadGistAsync from './url/load-gist-async.js';

const last = {
    hash: null
};
function save(code, options, { gist } = {}) {
    if (code == null) // too early?
        return {};

    if (gist && saveGist(code, options, gist))
        return { keepGist: true };

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
    return {};
}

function saveGist(code, options, gist) {
    if (code !== gist.code)
        return false;

    const normalize = o => o != null ? o : null;
    for (const key of ['language', 'target', 'branchId', 'release']) {
        if (normalize(options[key]) !== normalize(gist.options[key]))
            return false;
    }

    saveHash('gist:' + gist.id);
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
        return loadGistAsync(hash);

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
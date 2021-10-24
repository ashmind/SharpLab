import LZString from 'lz-string';
import type { RawOptions } from '../../types/raw-options';
import type { Gist } from '../../types/gist';
import { languages } from '../../helpers/languages';
import { targets } from '../../helpers/targets';
import warn from '../../helpers/warn';
import extendType from '../../helpers/extend-type';
import throwError from '../../helpers/throw-error';
import {
    languageMap,
    languageMapReverse,
    targetMap,
    targetMapReverse,
    targetMapReverseV1
} from './helpers/language-and-target-maps';
import precompressor from './url/precompressor';
import loadGistAsync from './url/load-gist-async';

const last = {
    hash: null as string|null
};
function save(code: string|null|undefined, options: RawOptions, { gist = null }: { gist?: Gist|null } = {}) {
    if (code == null) // too early?
        return {};

    if (gist && saveGist(code, options, gist))
        return { keepGist: true };

    const optionsPacked = {
        b: options.branchId,
        l: options.language !== languages.csharp ? languageMap[options.language] : null,
        t: options.target !== targets.csharp ? targetMap[options.target] : null,
        d: options.release ? '' : '+'
    };
    const optionsPackedString = Object
        .entries(optionsPacked)
        .filter(([, value]) => !!value)
        .map(([key, value]) => key + ':' + value) // eslint-disable-line prefer-template
        .join(',');
    const precompressedCode = precompressor.compress(code, options.language);
    const hash = 'v2:' + LZString.compressToBase64(optionsPackedString + '|' + precompressedCode); // eslint-disable-line prefer-template

    saveHash(hash);
    return {};
}

function saveGist(code: string, options: RawOptions, gist: Gist) {
    if (code !== gist.code)
        return false;

    const normalize = <T>(o: T|null|undefined) => o != null ? o : null;
    for (const key of ['language', 'target', 'branchId', 'release'] as const) {
        if (normalize(options[key]) !== normalize(gist.options[key]))
            return false;
    }

    saveHash('gist:' + gist.id);
    return true;
}

function saveHash(hash: string) {
    last.hash = hash;
    history.replaceState(null, '', '#' + hash);
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
        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        const [, optionsPart, codePart] = /^([^|]*)\|([\s\S]*)$/.exec(decompressed)!;

        const optionsPacked = (
            optionsPart.split(',').reduce((result, p) => {
                const [key, value] = p.split(':', 2);
                result[key] = value;
                return result;
            }, {} as { [key: string]: string|undefined })
        );
        const language = languageMapReverse[optionsPacked.l ?? 'cs']
                      // eslint-disable-next-line @typescript-eslint/restrict-template-expressions
                      ?? throwError(`Failed to resolve language: ${optionsPacked.l}`);
        const target = targetMapReverse[optionsPacked.t ?? 'cs']
                    // eslint-disable-next-line @typescript-eslint/restrict-template-expressions
                    ?? throwError(`Failed to resolve target: ${optionsPacked.t}`);
        const code = precompressor.decompress(codePart, language);
        return {
            options: {
                branchId: optionsPacked.b,
                language,
                target,
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

function changed(callback: () => void) {
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

function legacyLoadFrom(hash: string) {
    const match = /(?:b:([^/]+)\/)?(?:f:([^/]+)\/)?(.+)/.exec(hash);
    if (match === null)
        return null;

    // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition
    const flags = (match[2] ?? '').match(/^([^>]*?)(>.+?)?(r)?$/) ?? [];
    const result = extendType({
        options: {
            branchId: match[1],
            // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition
            language: languageMapReverse[flags[1] || 'cs'],
            // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition
            target: targetMapReverseV1[flags[2] || '>cs'],
            release: flags[3] === 'r'
        }
    })<{ code?: string }>();

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
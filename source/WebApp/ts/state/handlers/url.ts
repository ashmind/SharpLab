import LZString from 'lz-string';
import {
    encode as encodeArrayBufferToBase64,
    decode as decodeArrayBufferFromBase64
} from 'base64-arraybuffer';
import type { RawOptions } from '../../types/raw-options';
import type { Gist } from '../../types/gist';
import { LanguageName, languages } from '../../helpers/languages';
import { TargetName, targets } from '../../helpers/targets';
import warn from '../../helpers/warn';
import throwError from '../../helpers/throw-error';
import {
    languageMap,
    languageMapReverse,
    targetMap,
    targetMapReverse
} from './helpers/language-and-target-maps';
import precompressor from './url/precompressor';
import loadGistAsync, { LoadStateFromGistResult } from './url/load-gist-async';
import { loadFromLegacyV1, LoadStateFromUrlV1Result } from './url/load-from-v1';
import { loadFromLegacyV2, LoadStateFromUrlV2Result } from './url/load-from-v2';

let lastHash: string|undefined;

export const saveStateToUrl = (
    code: string|null|undefined,
    options: RawOptions,
    { cacheSecret, gist = null }: { cacheSecret: ArrayBuffer, gist?: Gist|null }
) => {
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
    const hash = 'v3:' + LZString.compressToBase64(optionsPackedString + '|' + precompressedCode) + '.' + encodeArrayBufferToBase64(cacheSecret); // eslint-disable-line prefer-template

    saveHash(hash);
    return {};
};

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
    lastHash = hash;
    history.replaceState(null, '', '#' + hash);
}

type LoadStateFromUrlV3Result = {
    readonly options: {
        readonly branchId: string | undefined,
        readonly language: LanguageName,
        readonly target: TargetName,
        readonly release: boolean
    },
    readonly code: string,
    readonly cacheSecret: ArrayBuffer
} | null;

type LoadStateFromUrlResult =
    Promise<LoadStateFromGistResult>
    | LoadStateFromUrlV1Result
    | LoadStateFromUrlV2Result
    | LoadStateFromUrlV3Result;

export const loadStateFromUrlAsync = (): LoadStateFromUrlResult => {
    let hash = getCurrentHash();
    if (!hash)
        return null;

    lastHash = hash;
    if (hash.startsWith('gist:'))
        return loadGistAsync(hash);

    if (!/^v\d:/.test(hash))
        return loadFromLegacyV1(hash);

    if (hash.startsWith('v2'))
        return loadFromLegacyV2(hash);

    hash = hash.substring('v3:'.length);
    try {
        const [data, cacheSecretString] = hash.split('.');
        const decompressed = LZString.decompressFromBase64(data);
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
        const cacheSecret = decodeArrayBufferFromBase64(cacheSecretString);
        return {
            options: {
                branchId: optionsPacked.b,
                language,
                target,
                release:  optionsPacked.d !== '+'
            },
            code,
            cacheSecret
        } as LoadStateFromUrlV3Result;
    }
    catch (e) {
        warn('Failed to load state from URL:', e);
        return null;
    }
};

export const subscribeToUrlStateChanged = (callback: () => void) => {
    window.addEventListener('hashchange', () => {
        const hash = getCurrentHash();
        if (hash !== lastHash)
            callback();
    });
};

function getCurrentHash() {
    const hash = window.location.hash;
    if (!hash)
        return null;
    return decodeURIComponent(hash.replace(/^#/, ''));
}
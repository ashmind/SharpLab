import LZString from 'lz-string';
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
    targetMapReverse,
    targetMapReverseV1
} from './helpers/language-and-target-maps';
import precompressor from './url/precompressor';
import loadGistAsync, { StateLoadedFromGist } from './url/load-gist-async';
import { fromBase64Url, toBase64Url } from './url/base64url';

type OptionsLoadedFromLegacyUrlV1 = {
    language: LanguageName|undefined;
    target: TargetName|undefined;
    release: boolean;
    branchId: string|undefined;
};

type StateLoadedFromLegacyUrlV1 = {
    code: string;
    options: OptionsLoadedFromLegacyUrlV1;
};

type OptionsLoadedFromUrl = {
    language: LanguageName;
    target: TargetName;
    release: boolean;
    branchId: string|undefined;
};

export type StateLoadedFromUrl = {
    code: string;
    options: OptionsLoadedFromUrl;
} | StateLoadedFromLegacyUrlV1 | StateLoadedFromGist;

// u stands for "You" since it's your code
const pathPrefix = '/u/';
const gistPrefix = 'gist-';

const last = {
    dataString: null as string|null
};
export function saveStateToUrl(
    code: string|null|undefined,
    options: RawOptions,
    { gist = null }: { gist?: Gist|null } = {}
) {
    if (code == null) // too early?
        return {};

    if (gist && saveGistToPath(code, options, gist))
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
    const dataString = LZString.compressToBase64(optionsPackedString + '|' + precompressedCode); // eslint-disable-line prefer-template

    saveDataStringToPath(dataString);
    return {};
}

function saveGistToPath(code: string, options: RawOptions, gist: Gist) {
    if (code !== gist.code)
        return false;

    const normalize = <T>(o: T|null|undefined) => o != null ? o : null;
    for (const key of ['language', 'target', 'branchId', 'release'] as const) {
        if (normalize(options[key]) !== normalize(gist.options[key]))
            return false;
    }

    saveDataStringToPath(gistPrefix + gist.id);
    return true;
}

function saveDataStringToPath(dataString: string) {
    last.dataString = dataString;
    history.replaceState(null, '', pathPrefix + toBase64Url(dataString));
}

export function loadStateFromUrlAsync(): Promise<StateLoadedFromUrl>|StateLoadedFromUrl|null {
    const path = window.location.pathname;
    if (!path || path === '/')
        return loadFromLegacyHashAsync();

    const pathString = decodeURIComponent(path.substring(pathPrefix.length));
    if (pathString.startsWith(gistPrefix))
        return loadGistAsync(pathString.substring(gistPrefix.length));

    return loadFromDataString(pathString);
}

function loadFromLegacyHashAsync() : Promise<StateLoadedFromUrl>|StateLoadedFromUrl|null {
    const { hash } = window.location;
    if (!hash)
        return null;

    const hashString = decodeURIComponent(hash.replace(/^#/, ''));
    if (hashString.startsWith('gist:'))
        return loadGistAsync(hashString.substring('gist:'.length));

    if (!hashString.startsWith('v2:'))
        return loadFromLegacyV1HashString(hash);

    return loadFromDataString(hashString.substring('v2:'.length));
}

function loadFromDataString(dataString: string): StateLoadedFromUrl|null {
    last.dataString = dataString;
    try {
        const decompressed = LZString.decompressFromBase64(fromBase64Url(dataString));
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
            code,
            options: {
                branchId: optionsPacked.b,
                language,
                target,
                release:  optionsPacked.d !== '+'
            }
        };
    }
    catch (e) {
        warn('Failed to load state from URL:', e);
        return null;
    }
}

function loadFromLegacyV1HashString(hash: string): StateLoadedFromLegacyUrlV1|null {
    const match = /(?:b:([^/]+)\/)?(?:f:([^/]+)\/)?(.+)/.exec(hash);
    if (match === null)
        return null;

    // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition
    const flags = (match[2] ?? '').match(/^([^>]*?)(>.+?)?(r)?$/) ?? [];

    const code = (() => {
        try {
            return LZString.decompressFromBase64(match[3]);
        }
        catch (e) {
            return '';
        }
    })();

    return {
        code,
        options: {
            branchId: match[1],
            // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition
            language: languageMapReverse[flags[1] || 'cs'],
            // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition
            target: targetMapReverseV1[flags[2] || '>cs'],
            release: flags[3] === 'r'
        }
    };
}
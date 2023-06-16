import LZString from 'lz-string';
import type { LanguageName } from '../../../../shared/languages';
import type { TargetName } from '../../../../shared/targets';
import {
    languageMapReverse,
    targetMapReverseV1
} from '../helpers/language-and-target-maps';

export type LoadStateFromUrlV1Result = {
    readonly options: {
        readonly branchId: string | undefined;
        readonly language: LanguageName | undefined;
        readonly target: TargetName | undefined;
        readonly release: boolean;
    };
    readonly code: string;
} | null;

export const loadFromLegacyV1 = (hash: string): LoadStateFromUrlV1Result => {
    const match = /(?:b:([^/]+)\/)?(?:f:([^/]+)\/)?(.+)/.exec(hash);
    if (match === null)
        return null;

    const flags = (match[2] ?? '').match(/^([^>]*?)(>.+?)?(r)?$/) ?? [];
    const code = (() => {
        try {
            // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
            return LZString.decompressFromBase64(match[3]!);
        }
        catch (e) {
            return '';
        }
    })();

    return {
        options: {
            branchId: match[1],
            language: languageMapReverse[flags[1] ?? 'cs'],
            target: targetMapReverseV1[flags[2] ?? '>cs'],
            release: flags[3] === 'r'
        },
        code
    };
};
import LZString from 'lz-string';
import type { LanguageName } from '../../../../app/shared/languages';
import type { TargetName } from '../../../../app/shared/targets';
import {
    languageMapReverse,
    targetMapReverseV1
} from '../helpers/language-and-target-maps';

export type LoadStateFromUrlV1Result = {
    readonly options: {
        readonly branchId: string;
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
        options: {
            branchId: match[1],
            // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition
            language: languageMapReverse[flags[1] || 'cs'],
            // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition
            target: targetMapReverseV1[flags[2] || '>cs'],
            release: flags[3] === 'r'
        },
        code
    };
};
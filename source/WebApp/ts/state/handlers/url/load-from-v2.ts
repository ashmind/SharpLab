import LZString from 'lz-string';
import type { LanguageName } from '../../../helpers/languages';
import type { TargetName } from '../../../helpers/targets';
import throwError from '../../../helpers/throw-error';
import warn from '../../../helpers/warn';
import { languageMapReverse, targetMapReverse } from '../helpers/language-and-target-maps';
import precompressor from './precompressor';

export type LoadStateFromUrlV2Result = {
    readonly options: {
        readonly branchId: string | undefined,
        readonly language: LanguageName,
        readonly target: TargetName,
        readonly release: boolean
    },
    readonly code: string
} | null;

export const loadFromLegacyV2 = (hash: string): LoadStateFromUrlV2Result => {
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
};
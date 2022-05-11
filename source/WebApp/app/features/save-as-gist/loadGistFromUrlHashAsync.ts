import { targetMapReverse } from '../../../ts/state/handlers/helpers/language-and-target-maps';
import type { LanguageName } from '../../shared/languages';
import type { TargetName } from '../../shared/targets';
import type { Gist } from './gist';
import { getGistAsync } from './github-client/gists';

interface Overrides {
    readonly target: TargetName | undefined;
    readonly branchId: string | undefined;
    readonly mode: 'debug' | 'release' | null;
}

export type LoadStateFromGistResult = {
    readonly gist: Gist;
    readonly options: {
        readonly language: LanguageName;
        readonly target: TargetName;
        readonly release: boolean;
        readonly branchId: string | null;
    };
    readonly code: string;
} | {
    readonly options: {
        readonly [key: string]: undefined;
    };
    readonly code: string;
};

function getIsRelease(options: { release: boolean|null|undefined }, overrides: Overrides) {
    if (overrides.mode || options.release == null)
        return overrides.mode !== 'debug';

    return options.release;
}

export default async function loadGistFromUrlHashAsync(hash: string): Promise<LoadStateFromGistResult> {
    const parts = hash.replace(/^gist:/, '').split('/');
    const id = parts[0];
    let gist;
    try {
        gist = await getGistAsync(id);
    }
    catch (e) {
        const message = `Failed to load gist '${id}': ${(e as { json?: { message?: string } }).json?.message ?? '<unknown>'}.`;
        return {
            code: message.replace(/^/mg, '#error '),
            options: {}
        };
    }

    // legacy feature: overriding gist settings through URL.
    // Only keeping this for permalink support.
    const overrides = {
        target: targetMapReverse[parts[1]],
        branchId: parts[2],
        mode: parts.length > 1 ? (parts[3] ?? 'release') : null
    } as Overrides;

    return {
        gist,
        code: gist.code,
        options: {
            language: gist.options.language,
            target:   overrides.target   ?? gist.options.target,
            branchId: overrides.branchId ?? gist.options.branchId,
            release:  getIsRelease(gist.options, overrides)
        }
    };
}
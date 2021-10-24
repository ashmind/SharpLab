import { TargetName, targets } from '../../../helpers/targets';
import { getGistAsync, GistGetResult } from '../../../helpers/github/gists';
import { targetMapReverse } from '../helpers/language-and-target-maps';
import type { LanguageName } from '../../../helpers/languages';

interface Overrides {
    target: TargetName|undefined;
    branchId: string|undefined;
    mode: 'debug'|'release'|null;
}

type FailedStateFromGist = {
    code: string;
    options?: undefined;
};

export type StateLoadedFromGist = {
    code: string;
    gist: GistGetResult;
    options: {
        language: LanguageName;
        target: string;
        release: boolean;
        branchId: string|undefined;
    }
} | FailedStateFromGist;


function getIsRelease(options: { release: boolean|null|undefined }, overrides: Overrides) {
    if (overrides.mode || options.release == null)
        return overrides.mode !== 'debug';

    return options.release;
}

export default async function loadGistAsync(hash: string) : Promise<StateLoadedFromGist> {
    const parts = hash.replace(/^gist:/, '').split('/');
    const id = parts[0];
    let gist;
    try {
        gist = await getGistAsync(id);
    }
    catch (e) {
        const message = `Failed to load gist '${id}': ${(e as { json?: { message?: string } }).json?.message ?? '<unknown>'}.`;
        return {
            code: message.replace(/^/mg, '#error ')
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
            target:   overrides.target   ?? gist.options.target ?? targets.csharp,
            branchId: overrides.branchId ?? gist.options.branchId,
            release:  getIsRelease(gist.options, overrides)
        }
    };
}
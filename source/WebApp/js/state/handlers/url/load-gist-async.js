import { getGistAsync } from '../../../helpers/github/gists.js';
import languages from '../../../helpers/languages.js';
import { languageAndTargetMapReverse } from '../helpers/language-and-target-maps.js';

function getIsRelease(options, overrides) {
    if (overrides.mode || options.release == null)
        return overrides.mode !== 'debug';

    return options.release;
}

export default async function loadGistAsync(hash) {
    const parts = hash.replace(/^gist:/, '').split('/');
    const id = parts[0];
    let gist;
    try {
        gist = await getGistAsync(id);
    }
    catch (e) {
        const message = `Failed to load gist '${id}': ${e.json.message}.`;
        return {
            code: message.replace(/^/mg, '#error '),
            options: {}
        };
    }

    // legacy feature: overriding gist settings through URL.
    // Only keeping this for permalink support.
    const overrides = {
        target: languageAndTargetMapReverse[parts[1]],
        branchId: parts[2],
        mode: parts.length > 1 ? (parts[3] || 'release') : null
    };

    return {
        gist,
        code: gist.code,
        options: {
            language: gist.options.language,
            target:   overrides.target   || gist.options.target || languages.csharp,
            branchId: overrides.branchId || gist.options.branchId,
            release:  getIsRelease(gist.options, overrides)
        }
    };
}
import { atom } from 'recoil';
import { toOptionsData } from '../persistent-state/handlers/helpers/optionsData';
import { saveStateToUrl } from '../persistent-state/handlers/url';
import type { Gist } from './Gist';

export const gistState = atom<Gist | null>({
    key: 'gist',
    default: null,
    effects: [
        ({ onSet }) => {
            onSet(gist => {
                if (!gist)
                    return;
                const { language, branchId, target, release } = gist.options;
                saveStateToUrl(gist.code, toOptionsData(
                    language,
                    branchId ? { id: branchId } : null,
                    target,
                    release
                ), { gist });
            });
        }
    ]
});
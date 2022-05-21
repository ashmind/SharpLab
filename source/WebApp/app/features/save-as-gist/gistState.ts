import { atom } from 'recoil';
import { saveStateToUrl } from '../persistent-state/handlers/url';
import type { Gist } from './gist';

export const gistState = atom<Gist | null>({
    key: 'gist',
    default: null,
    effects: [
        ({ onSet }) => {
            onSet(gist => {
                if (!gist)
                    return;
                saveStateToUrl(gist.code, gist.options, { gist });
            });
        }
    ]
});
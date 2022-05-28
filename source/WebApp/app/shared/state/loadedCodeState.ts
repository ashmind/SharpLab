import { atom } from 'recoil';

export const loadedCodeState = atom<string>({
    key: 'app-loaded-code',
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    default: null!
});
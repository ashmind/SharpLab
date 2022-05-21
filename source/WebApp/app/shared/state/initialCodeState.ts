import { atom } from 'recoil';

export const initialCodeState = atom<string>({
    key: 'app-initial-code',
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    default: null!,
    effects: []
});
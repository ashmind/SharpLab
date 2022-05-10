import { atom } from 'recoil';

export const codeState = atom({
    key: 'app-code',
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    default: null! as string,
    effects: []
});
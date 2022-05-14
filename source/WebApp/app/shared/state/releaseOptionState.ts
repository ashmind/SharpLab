import { atom } from 'recoil';

export const releaseOptionState = atom({
    key: 'app-options-release',
    default: false,
    effects: []
});
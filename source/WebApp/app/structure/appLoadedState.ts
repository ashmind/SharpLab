import { atom } from 'recoil';

export const appLoadedState = atom({
    key: 'app-loaded',
    default: false,
    effects: []
});
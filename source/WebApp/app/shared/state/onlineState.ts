import { atom } from 'recoil';

export const onlineState = atom<boolean>({
    key: 'app-online',
    default: true
});
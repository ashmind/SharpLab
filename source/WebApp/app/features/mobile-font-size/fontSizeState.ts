import { atom } from 'recoil';

const LOCAL_STORAGE_KEY = 'sharplab.settings.mobile-font-size';

export type MobileFontSize = 'default'|'large';

export const fontSizeState = atom<MobileFontSize>({
    key: 'mobile-font-size',
    default: localStorage[LOCAL_STORAGE_KEY] ?? 'default',
    effects: [
        ({ onSet }) => onSet(value => localStorage[LOCAL_STORAGE_KEY] = value.toString())
    ]
});
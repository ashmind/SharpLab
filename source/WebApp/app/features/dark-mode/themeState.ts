import { atom, selector } from 'recoil';

const LOCAL_STORAGE_KEY = 'sharplab.theme';
export type UserTheme = 'light'|'dark'|'auto';
type EffectiveTheme = 'light'|'dark';

/* eslint-disable @typescript-eslint/no-unnecessary-condition */
const systemDarkThemeQuery = window.matchMedia
                          && window.matchMedia('(prefers-color-scheme: dark)') as MediaQueryList|undefined;
/* eslint-enable @typescript-eslint/no-unnecessary-condition */

export const userThemeState = atom<UserTheme>({
    key: 'user-theme',
    default: 'auto',
    effects: [
        ({ setSelf, onSet }) => {
            const loaded = localStorage[LOCAL_STORAGE_KEY];
            if (loaded)
                setSelf(loaded);
            onSet(value => localStorage[LOCAL_STORAGE_KEY] = value.toString());
        }
    ]
});

const systemThemeState = atom<EffectiveTheme>({
    key: 'system-theme',
    effects: [
        ({ setSelf }) => {
            const updateSelf = () => setSelf(systemDarkThemeQuery?.matches ? 'dark' : 'light');
            updateSelf();
            /*
                TODO: upgrade to addEventListener once Edge/Safari support it.
                Polyfill does not seem possible due to Safari not exposing
                window.MediaQueryList.
            */
            systemDarkThemeQuery?.addListener(() => updateSelf());
        }
    ]
});

export const effectiveThemeSelector = selector({
    key: 'effective-theme',
    get: ({ get }) => {
        const userTheme = get(userThemeState);
        return userTheme === 'auto'
            ? get(systemThemeState)
            : userTheme;
    }
});

/*
function trackDarkTheme(effectiveTheme: EffectiveTheme) {
    if (userTheme === 'dark') {
        trackFeature('Theme: Dark (manual)');
    }
    else if (effectiveTheme === 'dark') {
        trackFeature('Theme: Dark (system)');
    }
}
*/
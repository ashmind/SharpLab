import type { AppTheme } from '../types/app';
import { save, load } from '../../ts/state/theme';
import trackFeature from './track-feature';

type EffectiveTheme = 'light'|'dark';

const watches = [] as Array<(value: EffectiveTheme) => void>;
/* eslint-disable @typescript-eslint/no-unnecessary-condition */
const systemDarkThemeQuery = window.matchMedia
                          && window.matchMedia('(prefers-color-scheme: dark)') as MediaQueryList|undefined;
/* eslint-enable @typescript-eslint/no-unnecessary-condition */

let userTheme = load() ?? 'auto';

function getEffectiveTheme(): EffectiveTheme {
    if (userTheme === 'auto')
        return systemDarkThemeQuery?.matches ? 'dark' : 'light';

    return userTheme;
}

function watchEffectiveTheme(callback: (value: EffectiveTheme) => void) {
    watches.push(callback);
    const index = watches.length;
    return () => {
        watches.splice(index, 1);
    };
}

function updateEffectiveThemeWatches(effective: EffectiveTheme) {
    for (const watch of watches) {
        watch(effective);
    }
}

function trackDarkTheme(effectiveTheme: EffectiveTheme) {
    if (userTheme === 'dark') {
        trackFeature('Theme: Dark (manual)');
    }
    else if (effectiveTheme === 'dark') {
        trackFeature('Theme: Dark (system)');
    }
}

trackDarkTheme(getEffectiveTheme());
watchEffectiveTheme(trackDarkTheme);

if (systemDarkThemeQuery) {
    /*
        TODO: upgrade to addEventListener once Edge/Safari support it.
        Polyfill does not seem possible due to Safari not exposing
        window.MediaQueryList.
    */
    systemDarkThemeQuery.addListener(() => {
        if (userTheme !== 'auto')
            return;
        updateEffectiveThemeWatches(getEffectiveTheme());
    });
}

export function setUserTheme(theme: AppTheme) {
    if (userTheme === theme)
        return;

    const oldEffectiveTheme = getEffectiveTheme();
    userTheme = theme;
    save(theme);

    const newEffectiveTheme = getEffectiveTheme();
    if (newEffectiveTheme !== oldEffectiveTheme)
        updateEffectiveThemeWatches(newEffectiveTheme);
}

export function getUserTheme() {
    return userTheme;
}

export { getEffectiveTheme, watchEffectiveTheme };
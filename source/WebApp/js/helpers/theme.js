import { save, load } from '../../js/state/theme.js';
import trackFeature from './track-feature.js';

/** @typedef {'light'|'dark'|'auto'} Theme */
/** @typedef {'light'|'dark'} EffectiveTheme */

/** @type {((value: EffectiveTheme) => void)[]} */
const watches = [];
const systemDarkThemeQuery = window.matchMedia
                          && window.matchMedia('(prefers-color-scheme: dark)');

/** @type {Theme} */
let userTheme = load() || 'auto';

/** @returns {EffectiveTheme} */
function getEffectiveTheme() {
    if (userTheme === 'auto')
        return (systemDarkThemeQuery && systemDarkThemeQuery.matches) ? 'dark' : 'light';

    return userTheme;
}

/**
 * @param {(value: EffectiveTheme) => void} callback
 * @returns {() => void}
 * */
function watchEffectiveTheme(callback) {
    watches.push(callback);
    const index = watches.length;
    return () => { watches.splice(index, 1); };
}

function updateEffectiveThemeWatches(effective) {
    for (const watch of watches) {
        watch(effective);
    }
}

/** @param {EffectiveTheme} effectiveTheme */
function trackDarkTheme(effectiveTheme) {
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
    systemDarkThemeQuery.addEventListener('change', () => {
        if (userTheme !== 'auto')
            return;
        updateEffectiveThemeWatches(getEffectiveTheme());
    });
}

/** @param {Theme} theme */
export function setUserTheme(theme) {
    if (userTheme === theme)
        return;

    const oldEffectiveTheme = getEffectiveTheme();
    userTheme = theme;
    save(theme);

    const newEffectiveTheme = getEffectiveTheme();
    if (newEffectiveTheme !== oldEffectiveTheme)
        updateEffectiveThemeWatches(newEffectiveTheme);
}

/** @returns {Theme} */
export function getUserTheme() {
    return userTheme;
}

export { getEffectiveTheme, watchEffectiveTheme };
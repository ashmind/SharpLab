export type Theme = 'light'|'dark'|'auto';

export function save(theme: Theme) {
    localStorage['sharplab.theme'] = theme;
}

export function load() {
    return localStorage['sharplab.theme'] as Theme|undefined;
}
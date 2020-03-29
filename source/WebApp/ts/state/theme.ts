export type Theme = 'light'|'dark'|'auto';

export function save(theme: Theme) {
    localStorage['sharplab.theme'] = theme;
}

export function load(): Theme|null {
    return localStorage['sharplab.theme'];
}
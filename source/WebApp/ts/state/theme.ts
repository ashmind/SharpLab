import type { AppTheme } from '../types/app';

export function save(theme: AppTheme) {
    localStorage['sharplab.theme'] = theme;
}

export function load() {
    return localStorage['sharplab.theme'] as AppTheme|undefined;
}
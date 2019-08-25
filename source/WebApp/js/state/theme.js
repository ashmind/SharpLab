export function save(theme) {
    localStorage['sharplab.theme'] = theme;
}

export function load() {
    return localStorage['sharplab.theme'];
}
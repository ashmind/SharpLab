export const classNames = (...names: ReadonlyArray<string|undefined|null|false>) => {
    return names.filter(n => n).join(' ');
};
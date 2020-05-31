import defineState from '../../ts/helpers/define-state';

export type MobileFontSize = 'default'|'large';

const storageKey = 'sharplab.settings.mobile-font-size';

const [mobileFontSize, setMobileFontSize] = defineState(
    localStorage[storageKey] as (MobileFontSize|null) ?? 'default',
    { beforeSet: size => { localStorage[storageKey] = size; } }
);

export { mobileFontSize, setMobileFontSize };
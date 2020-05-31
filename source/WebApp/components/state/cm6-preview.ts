import defineState from '../../ts/helpers/define-state';

const storageKey = 'sharplab.experiments.cm6preview';

const [cm6PreviewEnabled, setCM6PreviewEnabled] = defineState(
    localStorage[storageKey] === 'true',
    { beforeSet: value => { localStorage[storageKey] = value; } }
);

export { cm6PreviewEnabled, setCM6PreviewEnabled };
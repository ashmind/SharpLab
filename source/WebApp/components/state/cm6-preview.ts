const storageKey = 'sharplab.experiments.cm6preview';
const state = {
    enabled: localStorage[storageKey] === 'true'
};

export const cm6PreviewState = state as Readonly<typeof state>;
export function setCM6PreviewEnabled(value: boolean) {
    localStorage[storageKey] = value;
    state.enabled = value;
}
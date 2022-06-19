import { atom, selector } from 'recoil';

const STORAGE_KEY = 'sharplab.diagnostics.targets';

const diagnosticTargetsEnabledState = atom({
    key: 'diagnostic-targets-enabled-state',
    default: false,
    effects: [({ setSelf }) => {
        const queryValue = new URLSearchParams(window.location.search).get('diagnostics.targets');
        if (queryValue)
            localStorage.setItem(STORAGE_KEY, queryValue);

        setSelf(localStorage.getItem(STORAGE_KEY) === 'true');
    }]
});

export const diagnosticTargetsEnabledSelector = selector({
    key: 'diagnostic-targets-enabled',
    get: ({ get }) => get(diagnosticTargetsEnabledState)
});
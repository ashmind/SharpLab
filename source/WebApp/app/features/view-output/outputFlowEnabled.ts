const STORAGE_KEY = 'sharplab.experiments.output.flow';
const queryValue = new URLSearchParams(window.location.search).get('output.flow');
if (queryValue)
    localStorage.setItem(STORAGE_KEY, queryValue);

export const outputFlowEnabled = localStorage.getItem(STORAGE_KEY);
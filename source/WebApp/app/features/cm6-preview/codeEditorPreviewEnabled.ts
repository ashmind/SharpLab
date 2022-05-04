import { atom } from 'recoil';

const LOCAL_STORAGE_KEY = 'sharplab.experiments.cm6preview';

export const codeEditorPreviewEnabled = atom({
    key: 'cm6-preview-editorPreviewEnabled',
    default: false,
    effects: [
        ({ setSelf, onSet }) => {
            const loadedValue = localStorage[LOCAL_STORAGE_KEY] === 'true';
            setSelf(loadedValue);
            onSet(value => localStorage[LOCAL_STORAGE_KEY] = value.toString());
        }
    ]
});
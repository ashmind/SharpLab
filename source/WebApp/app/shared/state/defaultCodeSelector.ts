import { selector } from 'recoil';
import { getDefaultCode, isDefaultCode } from '../defaults';
import { languageOptionState } from './languageOptionState';
import { targetOptionState } from './targetOptionState';

export const defaultCodeSelector = selector({
    key: 'app-default-code',
    get: ({ get }) => {
        const language = get(languageOptionState);
        const target = get(targetOptionState);
        return getDefaultCode(language, target);
    }
});

export { isDefaultCode };
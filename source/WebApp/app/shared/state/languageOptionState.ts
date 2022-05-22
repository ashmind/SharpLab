import { atom } from 'recoil';
import type { LanguageName } from '../languages';

export const languageOptionState = atom<LanguageName>({
    key: 'app-options-language',
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    default: null!
});
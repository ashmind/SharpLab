import { atom } from 'recoil';
import type { Branch } from './types';

export const branchOptionState = atom<Branch | null>({
    key: 'app-options-branch',
    default: null,
    effects: []
});
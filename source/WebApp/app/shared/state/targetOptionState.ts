import { atom } from 'recoil';
import type { TargetName } from '../targets';

export const targetOptionState = atom<TargetName>({
    key: 'app-options-target',
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    default: null!
});
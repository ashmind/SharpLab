import { atom } from 'recoil';

type TargetOffset = number | null;

export const codeRangeSyncTargetState = atom<TargetOffset>({
    key: 'code-range-target',
    default: null
});
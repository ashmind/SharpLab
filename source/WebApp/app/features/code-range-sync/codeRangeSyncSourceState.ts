import { atom } from 'recoil';
import type { CodeRange } from '../../shared/CodeRange';

type SourceRange = CodeRange | {
    readonly start: number;
    readonly end: number;
} | null;

export const codeRangeSyncSourceState = atom<SourceRange>({
    key: 'code-range-source',
    default: null
});
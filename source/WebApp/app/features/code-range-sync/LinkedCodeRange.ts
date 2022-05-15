import type { CodeRange } from '../../shared/CodeRange';

export type LinkedCodeRange = {
    readonly source: CodeRange;
    readonly result: CodeRange;
};
import type { CodeRange } from '../../../ts/types/code-range';

export type LinkedCodeRange = {
    readonly source: CodeRange;
    readonly result: CodeRange;
};
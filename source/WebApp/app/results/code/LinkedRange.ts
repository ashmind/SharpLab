import type { CodeRange } from 'ts/types/code-range';

export type LinkedRange = {
    readonly source: CodeRange;
    readonly result: CodeRange;
};
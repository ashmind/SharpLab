import type { CodeRange } from './code-range';

export type HighlightedRange = CodeRange|{
    readonly start: number;
    readonly end: number;
};
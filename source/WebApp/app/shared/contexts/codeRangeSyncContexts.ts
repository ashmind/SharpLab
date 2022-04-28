import { createContext } from 'react';
import type { CodeRange } from 'ts/types/code-range';
import type { MutableContextValue } from './MutableContextValue';

export type SourceRange = CodeRange | {
    readonly start: number;
    readonly end: number;
} | null;
export type TargetOffset = number | null;

// eslint-disable-next-line @typescript-eslint/no-non-null-assertion
export const SourceRangeContext = createContext<MutableContextValue<SourceRange>>(null!);
// eslint-disable-next-line @typescript-eslint/no-non-null-assertion
export const TargetOffsetContext = createContext<MutableContextValue<TargetOffset>>(null!);
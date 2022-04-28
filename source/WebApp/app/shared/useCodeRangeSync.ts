import { Context, useContext } from 'react';
import { SourceRange, SourceRangeContext, TargetOffset, TargetOffsetContext } from './contexts/codeRangeSyncContexts';
import type { MutableContextValue } from './contexts/MutableContextValue';

export const useCodeRangeSync = (<T extends 'source'|'target'>(type: T) => useContext(
    (type === 'source' ? SourceRangeContext : TargetOffsetContext) as
        Context<MutableContextValue<SourceRange|TargetOffset>>
)) as {
    (type: 'source'): [SourceRange, (value: SourceRange) => void];
    (type: 'target'): [TargetOffset, (value: TargetOffset) => void];
};
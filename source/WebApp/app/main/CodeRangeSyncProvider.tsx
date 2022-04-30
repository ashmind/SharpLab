import React, { FC, ReactNode, useState } from 'react';
import { SourceRange, SourceRangeContext, TargetOffset, TargetOffsetContext } from '../shared/contexts/codeRangeSyncContexts';
import { MutableValueProvider } from './state/MutableValueProvider';

type Props = {
    children: ReactNode;
};

export const CodeRangeSyncProvider: FC<Props> = ({ children }) => {
    const [source, setSource] = useState<SourceRange>(null);
    const [target, setTarget] = useState<TargetOffset>(null);

    return <MutableValueProvider context={SourceRangeContext} value={source} setValue={setSource}>
        <MutableValueProvider context={TargetOffsetContext} value={target} setValue={setTarget}>
            {children}
        </MutableValueProvider>
    </MutableValueProvider>;
};
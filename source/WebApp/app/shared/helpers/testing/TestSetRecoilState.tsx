import React, { ReactNode, useEffect, useState } from 'react';
import { RecoilState, useSetRecoilState } from 'recoil';

type Props<T> = {
    state: RecoilState<T>;
    value: T;
    children?: ReactNode;
};

export const TestSetRecoilState = <T, >({ state, value, children = null }: Props<T>) => {
    const setState = useSetRecoilState(state);
    const [ready, setReady] = useState(false);
    useEffect(() => {
        setState(value);
        setReady(true);
    }, [setState, value]);

    return ready ? <>{children}</> : null;
};
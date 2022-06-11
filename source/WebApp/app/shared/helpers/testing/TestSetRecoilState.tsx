import { useEffect } from 'react';
import { RecoilState, useSetRecoilState } from 'recoil';

type Props<T> = {
    state: RecoilState<T>;
    value: T;
};

export const TestSetRecoilState = <T, >({ state, value }: Props<T>) => {
    const setState = useSetRecoilState(state);
    useEffect(() => setState(value), [setState, value]);
    return null;
};
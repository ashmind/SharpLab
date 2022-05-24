import type { MutableSnapshot, RecoilState } from 'recoil';

// eslint-disable-next-line @typescript-eslint/no-explicit-any
export const recoilTestState = <TAll extends Array<any>>(
    ...state: { [T in keyof TAll]: [RecoilState<TAll[T]>, TAll[T]] }
) => {
    return (snapshot: MutableSnapshot) => {
        for (const [key, value] of state) {
            snapshot.set(key, value);
        }
    };
};
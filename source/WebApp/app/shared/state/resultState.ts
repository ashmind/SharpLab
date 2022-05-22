import { useCallback } from 'react';
import { atom, selector, useSetRecoilState } from 'recoil';
import type { MaybeCached } from '../../features/result-cache/types';
import type { ParsedResult } from '../resultTypes';
import { resultReducer } from './results/resultReducer';
import type { ResultUpdateAction } from './results/ResultUpdateAction';

const resultState = atom<MaybeCached<ParsedResult> | undefined>({
    key: 'app-result',
    // eslint-disable-next-line no-undefined
    default: undefined
});

export const useDispatchResultUpdate = () => {
    const setResult = useSetRecoilState(resultState);
    return useCallback(
        (action: ResultUpdateAction) => setResult(resultReducer(action)),
        [setResult]
    );
};

export const resultSelector = selector({
    key: 'app-result-readonly',
    get: ({ get }) => get(resultState)
});
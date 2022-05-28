import React, { useEffect } from 'react';
import { useRecoilValue } from 'recoil';
import { DeepPartial, fromPartial } from '../../helpers/testing/fromPartial';
import type { ResultUpdateAction } from '../state/results/ResultUpdateAction';
import { resultSelector, useDispatchResultUpdate } from '../state/resultState';

type Props = {
    action: DeepPartial<ResultUpdateAction>;
    children: React.ReactNode;
};

export const ResultRoot: React.FC<Props> = ({ action, children }) => {
    const dispatchResultUpdate = useDispatchResultUpdate();
    const result = useRecoilValue(resultSelector);
    useEffect(() => dispatchResultUpdate(fromPartial(action)), [dispatchResultUpdate, action]);

    if (!result)
        return null;

    return <>{children}</>;
};
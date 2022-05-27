import React, { useEffect } from 'react';
import { useRecoilValue } from 'recoil';
import { DeepPartial, fromPartial } from '../../helpers/testing/fromPartial';
import type { ResultUpdateAction } from '../state/results/ResultUpdateAction';
import { resultSelector, useDispatchResultUpdate } from '../state/resultState';

type Props = {
    action: DeepPartial<ResultUpdateAction>;
    children: React.ReactNode;
    waitForFirstResult?: boolean;
};

export const ResultStateRoot: React.FC<Props> = ({ action, children, waitForFirstResult }) => {
    const dispatchResultUpdate = useDispatchResultUpdate();
    const result = useRecoilValue(resultSelector);
    useEffect(() => dispatchResultUpdate(fromPartial(action)), [dispatchResultUpdate, action]);

    if (!result && waitForFirstResult)
        return null;

    return <>{children}</>;
};
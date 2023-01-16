import React, { ReactNode, useEffect, useState } from 'react';
import { RecoilState, useRecoilSnapshot } from 'recoil';

type Props = {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    states: ReadonlyArray<RecoilState<any>>;
    children?: ReactNode;
};

export const TestWaitForRecoilStates: React.FC<Props> = ({ states, children = null }) => {
    const snapshot = useRecoilSnapshot();
    const [ready, setReady] = useState(false);

    useEffect(() => {
        for (const state of states) {
            // eslint-disable-next-line no-undefined
            if (snapshot.getLoadable(state).valueMaybe() === undefined)
                return;
        }
        setReady(true);
    }, [snapshot, states]);

    return ready ? <>{children}</> : null;
};
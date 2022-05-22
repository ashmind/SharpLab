import React, { type FC, type ReactNode } from 'react';
import { useRecoilValue } from 'recoil';
import { appLoadedState } from './appLoadedState';

type Props = {
    children: ReactNode;
};

export const AppLoader: FC<Props> = ({ children }) => {
    const loaded = useRecoilValue(appLoadedState);
    return loaded ? <>{children}</> : null;
};
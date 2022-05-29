import React from 'react';
import { useRecoilValue } from 'recoil';
import { appLoadedState } from './appLoadedState';

type Props = {
    children: React.ReactNode;
};

export const AppLoader: React.FC<Props> = ({ children }) => {
    const loaded = useRecoilValue(appLoadedState);
    return loaded ? <>{children}</> : null;
};
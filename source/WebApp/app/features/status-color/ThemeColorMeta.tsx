import React from 'react';
import { useRecoilValue } from 'recoil';
import { statusColorSelector } from './internal/statusColorSelector';

export const ThemeColorMeta = () => {
    const color = useRecoilValue(statusColorSelector);
    return <meta name="theme-color" content={color} />;
};
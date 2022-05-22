import React from 'react';
import { useRecoilValue } from 'recoil';
import { colorSelector } from './colorSelector';

export const ThemeColorMeta = () => {
    const color = useRecoilValue(colorSelector);
    return <meta name="theme-color" content={color} />;
};
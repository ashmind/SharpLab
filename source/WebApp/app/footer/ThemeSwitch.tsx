import { useIds } from 'app/helpers/useIds';
import React, { FC, useEffect, useState } from 'react';
import asLookup from 'ts/helpers/as-lookup';
import { getUserTheme, setUserTheme } from 'ts/helpers/theme';
import type { AppTheme } from 'ts/types/app';

const calculateCurrentLabel = () => ({
    auto:  'Auto',
    dark:  'Dark',
    light: 'Light'
} as const)[getUserTheme()];

const applyBodyClass = (theme: AppTheme) => {
    const allClasses = ['theme-dark', 'theme-auto'] as const;
    const newClassName = asLookup({
        dark: 'theme-dark',
        auto: 'theme-auto'
    } as const)[theme];

    const { body } = document;
    body.classList.remove(...allClasses);
    if (newClassName)
        body.classList.add(newClassName);
};

export const ThemeSwitch: FC = () => {
    const [currentLabel, setCurrentLabel] = useState<'Auto' | 'Dark' | 'Light'>(calculateCurrentLabel());
    const ids = useIds(['toggle']);

    const onClick = () => {
        const nextTheme = ({
            auto:  'dark',
            dark:  'light',
            light: 'auto'
        } as const)[getUserTheme()];

        setUserTheme(nextTheme);
        setCurrentLabel(calculateCurrentLabel());
        applyBodyClass(nextTheme);
    };
    useEffect(() => applyBodyClass(getUserTheme()), []);

    return <div className="theme-manager block-with-label">
        <label htmlFor={ids.toggle}>Theme:</label>
        <button onClick={onClick}
            id={ids.toggle}
            aria-label={`Theme Toggle, Current: ${currentLabel}`}>{currentLabel}</button>
    </div>;
};
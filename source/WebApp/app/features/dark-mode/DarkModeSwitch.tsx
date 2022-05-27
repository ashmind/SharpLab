import React, { useEffect, useId } from 'react';
import { useRecoilState } from 'recoil';
import { asLookup } from '../../helpers/asLookup';
import { UserTheme, userThemeState } from './themeState';

const themeLabels = {
    auto:  'Auto',
    dark:  'Dark',
    light: 'Light'
} as const;

const applyBodyClass = (theme: UserTheme) => {
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

export const DarkModeSwitch: React.FC = () => {
    const [userTheme, setUserTheme] = useRecoilState(userThemeState);
    const toggleId = useId();
    useEffect(() => applyBodyClass(userTheme), [userTheme]);
    useEffect(() => () => applyBodyClass('auto'), []);

    const onClick = () => {
        const nextTheme = ({
            auto:  'dark',
            dark:  'light',
            light: 'auto'
        } as const)[userTheme];
        setUserTheme(nextTheme);
    };

    const label = themeLabels[userTheme];
    return <div className="theme-manager block-with-label">
        <label htmlFor={toggleId}>Theme:</label>
        <button onClick={onClick}
            id={toggleId}
            aria-label={`Theme Toggle, Current: ${label}`}>{label}</button>
    </div>;
};
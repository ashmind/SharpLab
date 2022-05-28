import React, { useId } from 'react';
import { useRecoilState } from 'recoil';
import { asLookup } from '../../helpers/asLookup';
import { useDocumentBodyClass } from '../../helpers/useDocumentBodyClass';
import { userThemeState } from './themeState';

const themeLabels = {
    auto:  'Auto',
    dark:  'Dark',
    light: 'Light'
} as const;

export const DarkModeSwitch: React.FC = () => {
    const [userTheme, setUserTheme] = useRecoilState(userThemeState);
    const toggleId = useId();
    const themeClassName = asLookup({
        dark: 'theme-dark',
        auto: 'theme-auto'
    } as const)[userTheme];
    useDocumentBodyClass(themeClassName);

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
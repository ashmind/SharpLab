import React, { FC, useEffect, useId } from 'react';
import { useStoredState } from '../shared/useStoredState';

export type FontSize = 'default'|'large';

const applyBodyClass = (size: FontSize) => {
    document.body.classList.toggle(`mobile-font-size-large`, size === 'large');
};

export const MobileFontSizeSwitch: FC = () => {
    const [fontSize, setFontSize] = useStoredState<FontSize>('sharplab.settings.mobile-font-size', 'default');
    const toggleId = useId();

    const onClick = () => {
        setFontSize(s => s === 'default' ? 'large' : 'default');
    };
    useEffect(() => applyBodyClass(fontSize), [fontSize]);

    const label = fontSize === 'default' ? 'M' : 'L';
    return <div className="mobile-font-size-manager block-with-label">
        <label htmlFor={toggleId}>Font Size:</label>
        <button onClick={onClick}
            id={toggleId}
            aria-label={`Font Size Toggle, Current: ${label}`}>{label}</button>
    </div>;
};
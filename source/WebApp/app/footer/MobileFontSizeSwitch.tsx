import React, { FC, useEffect, useId, useState } from 'react';
import { MobileFontSize, mobileFontSize, setMobileFontSize } from '../../components/state/mobile-font-size';

const calculateCurrentLabel = () => ({
    default: 'M',
    large: 'L'
} as const)[mobileFontSize.value];

const applyBodyClass = (size: MobileFontSize) => {
    document.body.classList.toggle(`mobile-font-size-large`, size === 'large');
};

export const MobileFontSizeSwitch: FC = () => {
    const [currentLabel, setCurrentLabel] = useState<'M' | 'L'>(calculateCurrentLabel());
    const toggleId = useId();

    const onClick = () => {
        const newSize = mobileFontSize.value === 'default' ? 'large' : 'default';

        setMobileFontSize(newSize);
        setCurrentLabel(calculateCurrentLabel());
        applyBodyClass(newSize);
    };
    useEffect(() => applyBodyClass(mobileFontSize.value), []);

    return <div className="mobile-font-size-manager block-with-label">
        <label htmlFor={toggleId}>Font Size:</label>
        <button onClick={onClick}
            id={toggleId}
            aria-label={`Font Size Toggle, Current: ${currentLabel}`}>{currentLabel}</button>
    </div>;
};
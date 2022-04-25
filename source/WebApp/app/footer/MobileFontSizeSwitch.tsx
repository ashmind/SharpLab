import React, { FC, useEffect, useMemo, useState } from 'react';
import { uid } from 'ts/ui/helpers/uid';
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
    const id = useMemo(() => uid(), []);

    const onClick = () => {
        const newSize = mobileFontSize.value === 'default' ? 'large' : 'default';

        setMobileFontSize(newSize);
        setCurrentLabel(calculateCurrentLabel());
        applyBodyClass(newSize);
    };
    useEffect(() => applyBodyClass(mobileFontSize.value), []);

    return <div className="mobile-font-size-manager block-with-label">
        <label htmlFor={`mobile-font-size-toggle-${id}`}>Font Size:</label>
        <button onClick={onClick}
            id={`mobile-font-size-toggle-${id}`}
            aria-label={`Font Size Toggle, Current: ${currentLabel}`}>{currentLabel}</button>
    </div>;
};
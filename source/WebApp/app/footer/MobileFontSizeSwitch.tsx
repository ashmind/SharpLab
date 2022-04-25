import React, { FC, useEffect, useState } from 'react';
import { useIds } from 'app/helpers/useIds';
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
    const ids = useIds(['toggle']);

    const onClick = () => {
        const newSize = mobileFontSize.value === 'default' ? 'large' : 'default';

        setMobileFontSize(newSize);
        setCurrentLabel(calculateCurrentLabel());
        applyBodyClass(newSize);
    };
    useEffect(() => applyBodyClass(mobileFontSize.value), []);

    return <div className="mobile-font-size-manager block-with-label">
        <label htmlFor={ids.toggle}>Font Size:</label>
        <button onClick={onClick}
            id={ids.toggle}
            aria-label={`Font Size Toggle, Current: ${currentLabel}`}>{currentLabel}</button>
    </div>;
};
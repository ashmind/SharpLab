import React, { FC, useId, useState } from 'react';
import { cm6PreviewEnabled, setCM6PreviewEnabled } from '../../components/state/cm6-preview';

const calculateCurrentLabel = () => cm6PreviewEnabled.value ? 'Preview' : 'Default';

export const CodeEditorSwitch: FC = () => {
    const [currentLabel, setCurrentLabel] = useState<'Default' | 'Preview'>(calculateCurrentLabel());
    const toggleId = useId();

    const onClick = () => {
        setCM6PreviewEnabled(!cm6PreviewEnabled.value);
        setCurrentLabel(calculateCurrentLabel());
    };

    return <div className="cm6-preview-manager block-with-label">
        <label htmlFor={toggleId}>Editor:</label>
        <button id={toggleId}
            onClick={onClick}
            aria-label={`Editor Toggle, Current: ${currentLabel}`}>{currentLabel}</button>
    </div>;
};
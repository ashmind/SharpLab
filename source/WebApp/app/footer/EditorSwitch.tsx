import React, { FC, useState } from 'react';
import { cm6PreviewEnabled, setCM6PreviewEnabled } from 'components/state/cm6-preview';
import { useIds } from 'app/helpers/useIds';

const calculateCurrentLabel = () => cm6PreviewEnabled.value ? 'Preview' : 'Default';

export const EditorSwitch: FC = () => {
    const [currentLabel, setCurrentLabel] = useState<'Default' | 'Preview'>(calculateCurrentLabel());
    const ids = useIds(['toggle']);

    const onClick = () => {
        setCM6PreviewEnabled(!cm6PreviewEnabled.value);
        setCurrentLabel(calculateCurrentLabel());
    };

    return <div className="cm6-preview-manager block-with-label">
        <label htmlFor={ids.toggle}>Editor:</label>
        <button id={ids.toggle}
            onClick={onClick}
            aria-label={`Editor Toggle, Current: ${currentLabel}`}>{currentLabel}</button>
    </div>;
};
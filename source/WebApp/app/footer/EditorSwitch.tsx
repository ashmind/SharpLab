import React, { useMemo } from 'react';
import { cm6PreviewEnabled, setCM6PreviewEnabled } from 'components/state/cm6-preview';
import { uid } from 'ts/ui/helpers/uid';

const calculateCurrentLabel = () => cm6PreviewEnabled.value ? 'Preview' : 'Default';

export const EditorSwitch: React.FC = () => {
    const [currentLabel, setCurrentLabel] = React.useState<'Default' | 'Preview'>(calculateCurrentLabel());
    const id = useMemo(() => uid(), []);

    const onClick = () => {
        setCM6PreviewEnabled(!cm6PreviewEnabled.value);
        setCurrentLabel(calculateCurrentLabel());
    };

    return <div className="cm6-preview-manager block-with-label">
        <label htmlFor={`editor-toggle-${id}`}>Editor:</label>
        <button id={`editor-toggle-${id}`}
            onClick={onClick}
            aria-label={`Editor Toggle, Current: ${currentLabel}`}>{currentLabel}</button>
    </div>;
};
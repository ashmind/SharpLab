import React, { FC, useId } from 'react';
import { useRecoilState } from 'recoil';
import { codeEditorPreviewEnabled } from './codeEditorPreviewEnabled';

export const CodeEditorSwitch: FC = () => {
    const [enabled, setEnabled] = useRecoilState(codeEditorPreviewEnabled);
    const toggleId = useId();

    const onClick = () => setEnabled(e => !e);

    const label = enabled ? 'Preview' : 'Default';
    return <div className="cm6-preview-manager block-with-label">
        <label htmlFor={toggleId}>Editor:</label>
        <button id={toggleId}
            onClick={onClick}
            aria-label={`Editor Toggle, Current: ${label}`}>{label}</button>
    </div>;
};
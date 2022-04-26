import React, { FC } from 'react';
import { cm6PreviewEnabled } from 'components/state/cm6-preview';
import { CodeEditorProps, StableCodeEditor } from './StableCodeEditor';
import { PreviewCodeEditor } from './PreviewCodeEditor';

type Props = CodeEditorProps;
export { CodeEditorProps };

export const CodeEditor: FC<Props> = props => {
    return cm6PreviewEnabled.value
        ? <PreviewCodeEditor {...props} />
        : <StableCodeEditor {...props} />;
};
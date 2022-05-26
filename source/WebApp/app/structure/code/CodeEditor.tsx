import React from 'react';
import { useRecoilValue } from 'recoil';
import { codeEditorPreviewEnabled } from '../../features/cm6-preview/codeEditorPreviewEnabled';
import { PreviewCodeEditor } from '../../features/cm6-preview/PreviewCodeEditor';
import { CodeEditorProps, StableCodeEditor } from './StableCodeEditor';

type Props = CodeEditorProps;
export { CodeEditorProps };

export const CodeEditor: React.FC<Props> = props => {
    const editorPreviewEnabled = useRecoilValue(codeEditorPreviewEnabled);
    return editorPreviewEnabled
        ? <PreviewCodeEditor {...props} />
        : <StableCodeEditor {...props} />;
};
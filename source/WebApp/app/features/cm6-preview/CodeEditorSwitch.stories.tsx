import React from 'react';
import { RecoilRoot } from 'recoil';
import { codeEditorPreviewEnabled } from './codeEditorPreviewEnabled';
import { CodeEditorSwitch } from './CodeEditorSwitch';

export default {
    component: CodeEditorSwitch
};

type TemplateProps = {
    preview?: boolean;
};
const Template: React.FC<TemplateProps> = ({ preview } = {}) => <>
    <main />{/* needed for some styles to apply */}
    <footer>
        <RecoilRoot initializeState={s => s.set(codeEditorPreviewEnabled, !!preview)}>
            <CodeEditorSwitch />
        </RecoilRoot>
    </footer>
</>;

export const Default = () => <Template />;
export const Preview = () => <Template preview />;
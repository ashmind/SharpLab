import React from 'react';
import { TestSetRecoilState } from '../../shared/helpers/testing/TestSetRecoilState';
import { darkModeStory } from '../../shared/testing/darkModeStory';
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
        <TestSetRecoilState state={codeEditorPreviewEnabled} value={!!preview}>
            <CodeEditorSwitch />
        </TestSetRecoilState>
    </footer>
</>;

export const Default = () => <Template />;
export const Preview = () => <Template preview />;
export const DarkMode = darkModeStory(Default);
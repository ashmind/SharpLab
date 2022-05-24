import React from 'react';
import { RecoilRoot } from 'recoil';
import { DarkModeRoot } from '../../helpers/testing/DarkModeRoot';
import { recoilTestState } from '../../helpers/testing/recoilTestState';
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
        <RecoilRoot initializeState={recoilTestState([codeEditorPreviewEnabled, !!preview])}>
            <CodeEditorSwitch />
        </RecoilRoot>
    </footer>
</>;

export const Default = () => <Template />;
export const Preview = () => <Template preview />;
export const DarkMode = () => <DarkModeRoot><Template /></DarkModeRoot>;
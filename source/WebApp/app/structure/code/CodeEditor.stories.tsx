import React from 'react';
import { codeEditorPreviewEnabled } from '../../features/cm6-preview/codeEditorPreviewEnabled';
import { TestSetRecoilState } from '../../shared/helpers/testing/TestSetRecoilState';
import { TestWaitForRecoilStates } from '../../shared/helpers/testing/TestWaitForRecoilStates';
import { LanguageName, LANGUAGE_CSHARP } from '../../shared/languages';
import { languageOptionState } from '../../shared/state/languageOptionState';
import { loadedCodeState } from '../../shared/state/loadedCodeState';
import { CodeEditor } from './CodeEditor';

export default {
    component: CodeEditor
};

type TemplateProps = {
    preview?: boolean;
};
// eslint-disable-next-line @typescript-eslint/no-empty-function
const doNothing = () => {};
const Template: React.FC<TemplateProps> = ({ preview } = {}) =>
    <>
        <TestSetRecoilState state={codeEditorPreviewEnabled} value={!!preview} />
        <TestSetRecoilState state={languageOptionState} value={LANGUAGE_CSHARP as LanguageName} />
        <TestSetRecoilState state={loadedCodeState} value={'using System;\r\n\r\nConsole.WriteLine("🌄");'} />
        <TestWaitForRecoilStates states={[codeEditorPreviewEnabled, languageOptionState, loadedCodeState]}>
            <CodeEditor
                onCodeChange={doNothing}
                onConnectionChange={doNothing}
                onServerError={doNothing}
                onSlowUpdateResult={doNothing}
                onSlowUpdateWait={doNothing}
                executionFlow={null}
                initialCached />
        </TestWaitForRecoilStates>
    </>;

export const Default = () => <Template />;
export const Preview = () => <Template preview />;
import React from 'react';
import { LANGUAGE_CSHARP } from '../../../shared/languages';
import { DarkModeRoot } from '../../../shared/testing/DarkModeRoot';
import { EXAMPLE_CODE_WITH_EXECUTION_FLOW } from '../../../structure/code/StableCodeEditor.stories';
import { ExecutionFlowOutput } from './ExecutionFlowOutput';

export default {
    component: ExecutionFlowOutput
};

export const Full = () => <ExecutionFlowOutput
    flow={EXAMPLE_CODE_WITH_EXECUTION_FLOW.FLOW}
    code={EXAMPLE_CODE_WITH_EXECUTION_FLOW.CODE}
    language={LANGUAGE_CSHARP}
/>;
export const FullDarkMode = () => <DarkModeRoot><Full /></DarkModeRoot>;
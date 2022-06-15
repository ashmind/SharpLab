import React, { useMemo } from 'react';
import CodeMirror from 'codemirror';
import type { FlowStep } from '../../../shared/resultTypes';
import 'codemirror/addon/runmode/runmode';
import 'codemirror/mode/mllike/mllike';
import 'codemirror/mode/vb/vb';
import '../../../shared/codemirror/mode-cil';
import { LanguageName, LANGUAGE_CSHARP, LANGUAGE_FSHARP, LANGUAGE_IL, LANGUAGE_VB } from '../../../shared/languages';
import { assertType } from '../../../shared/helpers/assertType';

type Props = {
    code: string;
    language: LanguageName;
    flow: ReadonlyArray<FlowStep>;
};

const modeMap = {
    [LANGUAGE_CSHARP]: 'text/x-csharp',
    [LANGUAGE_VB]:     'text/x-vb',
    [LANGUAGE_IL]:     'text/x-cil',
    [LANGUAGE_FSHARP]: 'text/x-fsharp'
};
assertType<{ [K in LanguageName]: string }>(modeMap);

type ModeToken = {
    text: string;
    style: string | null;
};

export const ExecutionFlowOutput: React.FC<Props> = ({ code, flow, language }) => {
    const lines = useMemo(() => code.split(/\n/g), [code]);
    const mode = modeMap[language];

    const renderLine = (line: string) => {
        const parts = [] as Array<ModeToken>;
        CodeMirror.runMode(line, mode, (text, style) => parts.push({ text, style }));

        return parts.map(
            ({ text, style }) => style ? <span className={'cm-' + style}>{text}</span> : text
        );
    };

    const renderStep = (step: FlowStep, index: number) => {
        const line = lines[step.line - 1];
        return <div key={index} className="output-execution-step">
            <div>
                <span>{step.line}: </span>
                <code className='mirrorsharp-theme'>{renderLine(line)}</code>
            </div>
            {step.notes && <div className='output-execution-step-notes'>{step.notes}</div>}
        </div>;
    };

    return <div>{flow.map(renderStep)}</div>;
};
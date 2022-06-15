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

const groupSteps = (flow: ReadonlyArray<FlowStep>, lines: ReadonlyArray<string>) => {
    const groups = [] as Array<Array<FlowStep>>;
    for (const step of flow) {
        const lastGroup = groups[groups.length - 1];
        // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition
        const lastLine = lastGroup?.[lastGroup?.length - 1]?.line;
        if (lastLine <= step.line) {
            let matchLine = lastLine + 1;
            const skipped = [] as Array<FlowStep>;
            while (matchLine <= step.line && /^\s*$/.test(lines[matchLine - 1])) {
                skipped.push({ line: matchLine });
                matchLine += 1;
            }
            if (matchLine === step.line) {
                lastGroup.push(...skipped);
                lastGroup.push(step);
                continue;
            }
        }

        groups.push([step]);
    }
    return groups as ReadonlyArray<ReadonlyArray<FlowStep>>;
};

export const ExecutionFlowOutput: React.FC<Props> = ({ code, flow, language }) => {
    const lines = useMemo(() => code.split(/\n/g), [code]);
    const mode = modeMap[language];
    const groups = groupSteps(flow, lines);

    const renderLine = (line: string) => {
        const parts = [] as Array<ModeToken>;
        CodeMirror.runMode(line, mode, (text, style) => parts.push({ text, style }));

        return parts.map(
            // eslint-disable-next-line no-undefined
            ({ text, style }, index) => <span key={index} className={style ? ('cm-' + style) : undefined}>{text}</span>
        );
    };

    const renderStep = (step: FlowStep, index: number) => {
        const line = lines[step.line - 1];
        return <div key={index} className="output-execution-step">
            <div>
                <code className='output-execution-step-line mirrorsharp-theme'>{renderLine(line)}</code>
            </div>
            {step.notes && <div className='output-execution-step-notes'>{step.notes}</div>}
        </div>;
    };

    const renderGroup = (group: ReadonlyArray<FlowStep>, index: number) => {
        return <div key={index} className="output-execution-group">{group.map(renderStep)}</div>;
    };

    return <div>{groups.map(renderGroup)}</div>;
};
import React, { useMemo, useState } from 'react';
//import CodeMirror from 'codemirror';
import type { FlowStep } from '../../../shared/resultTypes';
import 'codemirror/addon/runmode/runmode';
import 'codemirror/mode/mllike/mllike';
import 'codemirror/mode/vb/vb';
import '../../../shared/codemirror/mode-cil';
import { LanguageName, LANGUAGE_CSHARP, LANGUAGE_FSHARP, LANGUAGE_IL, LANGUAGE_VB } from '../../../shared/languages';
import { assertType } from '../../../shared/helpers/assertType';
import { extractEvents, FlowEvent, FlowEventPart } from './flow/extractEvents';

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

/*type ModeToken = {
    text: string;
    style: string | null;
};*/

export const ExecutionFlowOutput: React.FC<Props> = ({ code/*, language*/, flow }) => {
    const [filter, setFilter] = useState('');
    const lines = useMemo(() => code.split(/\n/g), [code]);
    //const mode = modeMap[language];
    const events = useMemo(() => extractEvents(flow, lines), [flow, lines]);

    const filteredEvents = filter.length > 0
        ? events.filter(e => e.parts.some(p => p.text.includes(filter)))
        : events;

    const renderEventPart = (part: FlowEventPart, index: number) => {
        const className = `output-flow-event-part output-flow-event-part-${part.type}`;
        return <span className={className} key={index}>{part.text}</span>;
    };

    const renderEvent = (event: FlowEvent, index: number) => {
        return <li className='output-flow-event' key={index}>
            {event.parts.map(renderEventPart)}
        </li>;
    };

    return <div>
        <input type="search" value={filter} onChange={e => setFilter(e.target.value)} />
        <ol>{filteredEvents.map(renderEvent)}</ol>
    </div>;
};
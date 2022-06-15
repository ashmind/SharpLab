import { type OutputJsonLineFlow, parseFlow } from './internal/parse/parseFlow';

type OutputJsonLineData = OutputJsonLineFlow | object;

export const tryParseOutputJsonAsFlow = (data: OutputJsonLineData) => {
    if ('flow' in data)
        return parseFlow(data.flow);

    return null;
};
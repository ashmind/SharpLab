import { type OutputJsonLineFlow, parseFlowSteps } from './internal/parse/parseFlowSteps';

type OutputJsonLineData = OutputJsonLineFlow | object;

export const tryParseOutputJsonAsFlow = (data: OutputJsonLineData) => {
    if ('flow' in data)
        return parseFlowSteps(data.flow);

    return null;
};
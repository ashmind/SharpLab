import type { LinkedRange } from './LinkedRange';

const isLocationBetween = (location: CodeMirror.Position, start: CodeMirror.Position, end: CodeMirror.Position) => {
    if (location.line < start.line)
        return false;
    if (location.line === start.line && location.ch < start.ch)
        return false;
    if (location.line > end.line)
        return false;
    if (location.line === end.line && location.ch > end.ch)
        return false;
    return true;
};

export const findRange = (ranges: ReadonlyArray<LinkedRange>, location: CodeMirror.Position) => ranges.find(
    r => isLocationBetween(location, r.result.start, r.result.end)
) ?? null;
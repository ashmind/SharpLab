import type { LinkedCodeRange } from './LinkedCodeRange';

const isLocationBetween = (position: CodeMirror.Position, start: CodeMirror.Position, end: CodeMirror.Position) => {
    if (position.line < start.line)
        return false;
    if (position.line === start.line && position.ch < start.ch)
        return false;
    if (position.line > end.line)
        return false;
    if (position.line === end.line && position.ch > end.ch)
        return false;
    return true;
};

export const findRangeByTargetPosition = (ranges: ReadonlyArray<LinkedCodeRange>, position: CodeMirror.Position) => ranges.find(
    r => isLocationBetween(position, r.result.start, r.result.end)
) ?? null;
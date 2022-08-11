import type { CodeRange } from '../../CodeRange';

const regexp = /^(\s*)\/\/ sequence point: \(line (\d+), col (\d+)\) to \(line (\d+), col (\d+)\) in \S+/;

const endOfLastLine = (lines: ReadonlyArray<string>) => {
    return { line: lines.length - 1, ch: lines[lines.length - 1].length };
};

export const extractRangesFromIL = (code: string) => {
    const ranges = [];

    const [newline] = code.match(/\r\n|\r|\n/) ?? ['\n'];
    const lines = code.split(newline);
    const clean = [];

    let lastRange: {
        source: CodeRange;
        result: { start: CodeRange['start']; end?: CodeRange['end'] };
    }|null = null;
    let lastRangeIndent: string|undefined;
    for (const line of lines) {
        const lineNumber = clean.length;

        const match = line.match(regexp);
        if (match) {
            const [, indent, startLine, startCol, endLine, endCol] = match;
            if (lastRange)
                lastRange.result.end = endOfLastLine(clean);
            const range = {
                source: {
                    start: { line: parseInt(startLine, 10) - 1, ch: parseInt(startCol, 10) - 1 },
                    end:   { line: parseInt(endLine, 10) - 1,   ch: parseInt(endCol, 10) - 1   }
                } as CodeRange,
                result: {
                    start: { line: lineNumber, ch: indent.length }
                }
            };
            ranges.push(range);
            lastRange = range;
            lastRangeIndent = indent;
            continue;
        }

        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        if (lastRange && !line.startsWith(lastRangeIndent!)) {
            lastRange.result.end = endOfLastLine(clean);
            lastRange = null;
        }

        clean.push(line);
    }

    return {
        code: clean.join(newline),
        ranges: ranges as Array<{ source: CodeRange; result: CodeRange }>
    };
};
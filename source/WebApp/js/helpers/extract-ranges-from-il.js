const regexp = /^(\s*)\/\/ sequence point: \(line (\d+), col (\d+)\) to \(line (\d+), col (\d+)\) in \S+/;

function endOfLastLine(lines) {
    return { line: lines.length - 1, ch: lines[lines.length - 1].length };
}

export default function extractRangesFromIL(code) {
    const ranges = [];

    const [newline] = code.match(/\r\n|\r|\n/) || ['\n'];
    const lines = code.split(newline);
    const clean = [];

    let lastRange;
    let lastRangeIndent;
    for (const line of lines) {
        const lineNumber = clean.length;

        const match = line.match(regexp);
        if (match) {
            const [, indent, startLine, startCol, endLine, endCol] = match;
            if (lastRange)
                lastRange.result.end = endOfLastLine(clean);
            const range = {
                source: {
                    start: { line: parseInt(startLine) - 1, ch: parseInt(startCol) - 1 },
                    end:   { line: parseInt(endLine) - 1,   ch: parseInt(endCol) - 1   }
                },
                result: {
                    start: { line: lineNumber, ch: indent.length }
                }
            };
            ranges.push(range);
            lastRange = range;
            lastRangeIndent = indent;
            continue;
        }

        if (lastRange && !line.startsWith(lastRangeIndent)) {
            lastRange.result.end = endOfLastLine(clean);
            lastRange = null;
        }

        clean.push(line);
    }

    return { code: clean.join(newline), ranges };
}
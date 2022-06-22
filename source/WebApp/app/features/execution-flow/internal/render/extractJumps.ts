import type { SimpleLineDetails } from './processStepsIntoLineDetails';

export const extractJumps = (lines: ReadonlyArray<SimpleLineDetails>) => {
    const jumps = [] as Array<JumpData>;
    for (const { step, jumpTo } of lines) {
        if (!jumpTo)
            continue;
        jumps.push({ fromLine: step.line - 1, toLine: jumpTo.line - 1 });
    }

    console.log('linesForJumps', lines);
    console.log('jumps', jumps);
    return jumps;
};
import type { DeepPartial } from '../../../../shared/helpers/testing/fromPartial';
import type { FlowStep } from '../../../../shared/resultTypes';
import { FLOW_TAG_LOOP_END, FLOW_TAG_LOOP_START, FLOW_TAG_METHOD_RETURN, FLOW_TAG_METHOD_START } from '../tags';
import { LineDetails, processStepsIntoLineDetails } from './processStepsIntoLineDetails';

test('nested loops', () => {
    const steps = [
        { line: 1, tags: [FLOW_TAG_LOOP_START] },
        { line: 2, tags: [FLOW_TAG_LOOP_START] },
        { line: 3 },
        { line: 4, tags: [FLOW_TAG_LOOP_END] },
        { line: 5, tags: [FLOW_TAG_LOOP_END] }
    ] as ReadonlyArray<FlowStep>;

    const results = processStepsIntoLineDetails(steps);

    expect(results).toMatchObject([
        { line: 1, type: 'loop', visits: [{
            lines: [
                { line: 2, type: 'loop', visits: [{
                    lines: [
                        { line: 3, type: 'step' },
                        { line: 4, type: 'step' }
                    ]
                }] },
                { line: 5, type: 'step' }
            ]
        }] }
    ] as DeepPartial<ReadonlyArray<LineDetails>>);
});

test('unfinished parent loop with one iteration is inlined', () => {
    const steps = [
        { line: 1, tags: [FLOW_TAG_LOOP_START] },
        { line: 2, tags: [FLOW_TAG_LOOP_START] },
        { line: 3 },
        { line: 4, tags: [FLOW_TAG_LOOP_END] },
        { line: 5 }
    ] as ReadonlyArray<FlowStep>;

    const results = processStepsIntoLineDetails(steps);

    expect(results).toMatchObject([
        { line: 2, type: 'loop', visits: [{
            lines: [
                { line: 3, type: 'step' },
                { line: 4, type: 'step' }
            ]
        }] },
        { line: 5, type: 'step' }
    ] as DeepPartial<ReadonlyArray<LineDetails>>);
});

test('complex method-loop scenario', () => {
    let method2CallNumber = 1;
    const method2 = () => [
        // eslint-disable-next-line no-plusplus
        { line: 50, tags: [FLOW_TAG_METHOD_START], notes: `method: 2.${method2CallNumber++}` },
        { line: 51, tags: [FLOW_TAG_METHOD_RETURN], notes: `method: 2.${method2CallNumber}` }
    ];

    let loop1IterationNumber = 1;
    const loop1Start = () => (
        // eslint-disable-next-line no-plusplus
        { line: 2, tags: [FLOW_TAG_LOOP_START], notes: `loop: 1.${loop1IterationNumber++}` }
    );
    const loop1 = () => [
        loop1Start(),
        ...method2(),
        { line: 4, tags: [FLOW_TAG_LOOP_END], notes: `loop: 1.${loop1IterationNumber}` }
    ];

    let loop2IterationNumber = 1;
    const loop2Start = () => (
        // eslint-disable-next-line no-plusplus
        { line: 5, tags: [FLOW_TAG_LOOP_START], notes: `loop: 2.${loop2IterationNumber++}` }
    );
    const loop2 = () => [
        loop2Start(),
        ...method2(),
        { line: 7, tags: [FLOW_TAG_LOOP_END], notes: `loop: 1.${loop2IterationNumber}` }
    ];

    const steps = [
        { line: 1, tags: ['method-start'] },

        ...loop1(),
        ...loop1(),
        ...loop1(),
        ...loop1(),
        loop1Start(),

        ...loop2(),
        ...loop2(),
        ...loop2(),
        ...loop2(),
        loop2Start(),

        { line: 8, tags: [ 'method-return' ] }
    ] as ReadonlyArray<FlowStep>;

    const results = processStepsIntoLineDetails(steps);

    expect(results).toMatchObject([
        { line: 1, type: 'method', visits: [{
            lines: [
                { line: 3, type: 'step' },
                { line: 4, type: 'step' }
            ]
        }] },
        { line: 5, type: 'step' }
    ] as DeepPartial<ReadonlyArray<LineDetails>>);
});
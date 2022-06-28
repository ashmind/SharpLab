import type { FlowArea, FlowStep } from '../../../../shared/resultTypes';
import type { AreaVisitDetails, LineDetails, StepDetails } from './detailsTypes';
import { processFlowIntoLineDetails } from './processFlowIntoLineDetails';

const loop = (startLine: number, endLine: number): FlowArea =>
    ({ type: 'loop', startLine, endLine });
const steps = (values: ReadonlyArray<FlowStep | number>): ReadonlyArray<FlowStep> =>
    values.map(v => typeof v === 'number' ? ({ line: v }) : v);

const stepDetails = (value: StepDetails|number): StepDetails =>
    typeof value === 'number' ? (({ type: 'step', step: { line: value }, line: value })) : value;
const lineDetails = (value: LineDetails|number): LineDetails =>
    typeof value === 'number' ? stepDetails(value) : value;
const loopVisit = (area: FlowArea, start: StepDetails|number, lines: ReadonlyArray<LineDetails|number>): AreaVisitDetails =>
    ({ type: 'area', area, start: stepDetails(start), lines: lines.map(lineDetails) }) as Omit<AreaVisitDetails, 'order'> as AreaVisitDetails;

test('repeated loop', () => {
    const flow = {
        areas: [loop(1, 2)],
        steps: steps([1, 2, 1, 2])
    } as const;

    const results = processFlowIntoLineDetails(flow);

    expect(results.lines).toMatchObject([
        loopVisit(flow.areas[0], 1, [2]),
        loopVisit(flow.areas[0], 1, [2])
    ]);
});

test('nested loops', () => {
    const flow = {
        areas: [
            loop(1, 5),
            loop(2, 4)
        ],
        steps: steps([1, 2, 3, 4, 5])
    } as const;

    const results = processFlowIntoLineDetails(flow);

    expect(results.lines).toMatchObject([
        loopVisit(flow.areas[0], 1, [
            loopVisit(flow.areas[1], 2, [3, 4]),
            5
        ])
    ]);
});

test('adjacent loops', () => {
    const flow = {
        areas: [
            loop(1, 2),
            loop(3, 4)
        ],
        steps: steps([1, 2, 3, 4])
    } as const;

    const results = processFlowIntoLineDetails(flow);

    expect(results.lines).toMatchObject([
        loopVisit(flow.areas[0], 1, [2]),
        loopVisit(flow.areas[1], 3, [4])
    ]);
});

/*
test('unfinished parent loop with one iteration is inlined', () => {
    const steps = [
        { line: 1, tags: [FLOW_TAG_LOOP_START] },
        { line: 2, tags: [FLOW_TAG_LOOP_START] },
        { line: 3 },
        { line: 4, tags: [FLOW_TAG_LOOP_END] },
        { line: 5 }
    ] as ReadonlyArray<FlowStep>;

    const results = processFlowIntoLineDetails(steps);

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
});*/
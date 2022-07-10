import type { FlowArea, FlowStep } from '../../../../shared/resultTypes';
import type { AreaVisitDetails, LineDetails, StepDetails } from './detailsTypes';
import { processFlowIntoLineDetails } from './processFlowIntoLineDetails';

const loop = (startLine: number, endLine: number): FlowArea =>
    ({ type: 'loop', startLine, endLine });
const method = (startLine: number, endLine: number): FlowArea =>
    ({ type: 'method', startLine, endLine });
const steps = (values: ReadonlyArray<FlowStep | number>): ReadonlyArray<FlowStep> =>
    values.map(v => typeof v === 'number' ? ({ line: v }) : v);

const stepDetails = (value: StepDetails|number): StepDetails =>
    typeof value === 'number' ? (({ type: 'step', step: { line: value }, line: value })) : value;
const lineDetails = (value: LineDetails|number): LineDetails =>
    typeof value === 'number' ? stepDetails(value) : value;
const visitDetails = (area: FlowArea, start: StepDetails|number, lines: ReadonlyArray<LineDetails|number>): AreaVisitDetails =>
    ({ type: 'area', area, start: stepDetails(start), lines: lines.map(lineDetails) }) as Omit<AreaVisitDetails, 'order'> as AreaVisitDetails;

test('repeated loop', () => {
    const flow = {
        areas: [loop(1, 2)],
        steps: steps([1, 2, 1, 2])
    } as const;

    const results = processFlowIntoLineDetails(flow);

    expect(results.lines).toMatchObject([
        visitDetails(flow.areas[0], 1, [2]),
        visitDetails(flow.areas[0], 1, [2])
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
        visitDetails(flow.areas[0], 1, [
            visitDetails(flow.areas[1], 2, [3, 4]),
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
        visitDetails(flow.areas[0], 1, [2]),
        visitDetails(flow.areas[1], 3, [4])
    ]);
});

test('method call', () => {
    const flow = {
        areas: [method(11, 12)],
        steps: steps([1, 11, 12, 2])
    } as const;

    const results = processFlowIntoLineDetails(flow);

    expect(results.lines).toMatchObject([
        stepDetails(1),
        visitDetails(flow.areas[0], 11, [12]),
        stepDetails(2)
    ]);
});

test('method to method call', () => {
    const flow = {
        areas: [
            method(11, 12),
            method(21, 22)
        ],
        steps: steps([1, 11, 21, 22, 12, 2])
    } as const;

    const results = processFlowIntoLineDetails(flow);

    expect(results.lines).toMatchObject([
        stepDetails(1),
        visitDetails(flow.areas[0], 11, [
            visitDetails(flow.areas[1], 21, [22]),
            12
        ]),
        stepDetails(2)
    ]);
});

test('method call in loop', () => {
    const flow = {
        areas: [
            loop(2, 3),
            method(11, 12)
        ],
        steps: steps([1, 2, 11, 12, 3])
    } as const;

    const results = processFlowIntoLineDetails(flow);

    expect(results.lines).toMatchObject([
        stepDetails(1),
        visitDetails(flow.areas[0], 2, [
            visitDetails(flow.areas[1], 11, [12]),
            3
        ])
    ]);
});
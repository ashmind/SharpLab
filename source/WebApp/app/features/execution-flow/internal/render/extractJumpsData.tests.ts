import { fromPartial } from '../../../../shared/helpers/testing/fromPartial';
import { extractJumpsData } from './extractJumpsData';

test('ignores if from step is not on the list', () => {
    // arrange
    const steps = [{ step: {} }];
    const jumps = [{ from: {}, to: steps[0].step }];

    // act
    const results = extractJumpsData(fromPartial(jumps), fromPartial(steps));

    // assert
    expect(results).toMatchObject([]);
});

test('ignores if from step is on the list, but has jump-to-only', () => {
    // arrange
    const steps = [{ mode: 'jump-to-only', step: {} }, { step: {} }] as const;
    const jumps = [{ from: steps[0].step, to: steps[1].step }];

    // act
    const results = extractJumpsData(fromPartial(jumps), fromPartial(steps));

    // assert
    expect(results).toMatchObject([]);
});

test('ignores if to step is not on the list', () => {
    // arrange
    const steps = [{ step: {} }];
    const jumps = [{ from: steps[0].step, to: {} }];

    // act
    const results = extractJumpsData(fromPartial(jumps), fromPartial(steps));

    // assert
    expect(results).toMatchObject([]);
});

test('extracts if both from and to are on the list', () => {
    // arrange
    const steps = [{ step: { line: 10 } }, { step: { line: 20 } }] as const;
    const jumps = [{ from: steps[0].step, to: steps[1].step }];

    // act
    const results = extractJumpsData(fromPartial(jumps), fromPartial(steps));

    // assert
    expect(results).toMatchObject([{ fromLine: 9, toLine: 19 }]);
});
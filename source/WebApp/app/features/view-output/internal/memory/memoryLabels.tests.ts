import { calculateLabelLevels } from './memoryLabels';

test('calculateLabelLevels calculates padding correctly', async () => {
    const labels = [{ name: 'A', offset: 0, length: 1 }, { name: 'B', offset: 4, length: 4 }];
    const dataLength = 8;

    const levels = calculateLabelLevels(labels, dataLength);

    expect(levels).toMatchObject([[
        { name: 'A',  length: 1 },
        { length: 3 },
        { name: 'B',  length: 4 }
    ]]);
});
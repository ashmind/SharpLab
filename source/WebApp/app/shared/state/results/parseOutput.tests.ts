import { parseOutput } from './parseOutput';

describe('parseOutput', () => {
    test.each([
        [ 'test#{"flow":[1]}\n', {
            output: ['test'],
            flow: {
                steps: [{ line: 1 }],
                areas: []
            }
        } ],

        [ '#{"type":"inspection:simple","value":3}\ntest#{"flow":[1]}\n', {
            output: [
                { type: 'inspection:simple', value: 3 },
                'test'
            ],
            flow: {
                steps: [{ line: 1 }],
                areas: []
            }
        } ],

        [ '#{test}\ntest#{test}\n', {
            output: ['#{test}\ntest#{test}\n'],
            flow: null
        } ],

        [ 'abc#{"type":"inspection:simple"}\ndef', {
            output: ['abc', { type: 'inspection:simple' }, 'def'],
            flow: null
        } ]
    ] as const)("parses '%s' correctly", (outputString, expected) => {
        const parsed = parseOutput(outputString);

        expect(parsed).toEqual(expected);
    });
});
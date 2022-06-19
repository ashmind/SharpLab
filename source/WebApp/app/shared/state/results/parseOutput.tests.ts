import { parseOutput } from './parseOutput';

describe('parseOutput', () => {
    test.each([
        [ 'test#{"flow":[1]}\n', { output: ['test'], flow: [{ line: 1 }] } ],

        [ '#{"type":"inspection:simple","value":3}\ntest#{"flow":[1]}\n', {
            output: [
                { type: 'inspection:simple', value: 3 },
                'test'
            ],
            flow: [{ line: 1 }]
        } ],

        [ '#{test}\ntest#{test}\n', { output: ['#{test}\ntest#{test}\n'], flow: [] } ],

        [ 'abc#{"type":"inspection:simple"}\ndef', {
            output: ['abc', { type: 'inspection:simple' }, 'def'],
            flow: []
        } ]
    ] as const)("parses '%s' correctly", (outputString, expected) => {
        const parsed = parseOutput(outputString);

        expect(parsed).toEqual(expected);
    });
});
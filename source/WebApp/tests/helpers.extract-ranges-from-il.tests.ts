import extractRangesFromIL from '../ts/helpers/extract-ranges-from-il';

describe('extractRangesFromIL', () => {
    test('returns expected ranges and code for realistic IL', () => {
        const il = `
            .method public hidebysig
                instance void M () cil managed
            {
                // Method begins at RVA 0x2050
                // Code size 2 (0x2)
                .maxstack 8

                // sequence point: (line 3, col 21) to (line 3, col 22) in _
                IL_0000: nop
                // sequence point: (line 4, col 5) to (line 4, col 6) in _
                IL_0001: ret
            } // end of method C::M
        `.trim();

        const { code, ranges } = extractRangesFromIL(il);
        expect(code).toEqual(`
            .method public hidebysig
                instance void M () cil managed
            {
                // Method begins at RVA 0x2050
                // Code size 2 (0x2)
                .maxstack 8

                IL_0000: nop
                IL_0001: ret
            } // end of method C::M
        `.trim());
        expect(ranges).toMatchObject([
            {
                source: { start: { line: 2, ch: 20 }, end: { line: 2, ch: 21 } },
                result: { start: { line: 7 }, end: { line: 7 } }
            },
            {
                source: { start: { line: 3, ch: 4 }, end: { line: 3, ch: 5 } },
                result: { start: { line: 8 }, end: { line: 8 } }
            }
        ]);
    });

    for (const [name, il, expected] of ([
        ['sequence point on first line', '// sequence point: (line 1, col 2) to (line 3, col 4) in _\r\nIL_0000: nop', [0]]
    ] as const))
    test(`returns expected range targets for ${name}`, () => {
        const { ranges } = extractRangesFromIL(il);
        expect(ranges).toMatchObject(
            expected.map(e => ({ result: { start: { line: e }}}))
        );
    });
});
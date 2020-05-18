import { languages } from '../ts/helpers/languages';
import help from '../ts/helpers/help';
import precompressor from '../ts/state/handlers/url/precompressor';

describe('precompressor', () => {
    for (const [code, expected] of [
        [`// test\r\n${help.run.csharp}\r\n// test`, '// test\r\n@20\r\n// test'],
        ['public class C() {}', '@3 @2 C() {}']
    ] as const) {
        test(`compresses C# code ${code} to ${expected}`, () => {
            const compressed = precompressor.compress(code, languages.csharp);
            expect(compressed).toBe(expected);
        });
    }
});
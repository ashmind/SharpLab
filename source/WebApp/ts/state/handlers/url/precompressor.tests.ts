import { LANGUAGE_CSHARP } from '../../../../app/shared/languages';
import help from '../../../helpers/help';
import precompressor from './precompressor';

describe('precompressor', () => {
    for (const [code, expected] of [
        [`// test\r\n${help.run.csharp}\r\n// test`, '// test\r\n@20\r\n// test'],
        ['public class C() {}', '@3 @2 C() {}']
    ] as const) {
        test(`compresses C# code ${code} to ${expected}`, () => {
            const compressed = precompressor.compress(code, LANGUAGE_CSHARP);
            expect(compressed).toBe(expected);
        });
    }
});
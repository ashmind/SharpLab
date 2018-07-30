import languages from '../js/helpers/languages.js';
import help from '../js/helpers/help.js';
import precompressor from '../js/state/handlers/url/precompressor.js';

describe('precompressor', () => {
    for (const [code, expected] of [
        [`// test\r\n${help.run.csharp}\r\n// test`, '// test\r\n@20\r\n// test'],
        ['public class C() {}', '@3 @2 C() {}']
    ]) {
        test(`compresses C# code ${code} to ${expected}`, () => {
            const compressed = precompressor.compress(code, languages.csharp);
            expect(compressed).toBe(expected);
        });
    }
});
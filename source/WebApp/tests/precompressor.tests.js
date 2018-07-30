import * as getGistAsync from '../js/state/handlers/url/get-gist-async.js';
import languages from '../js/helpers/languages.js';
import targets from '../js/helpers/targets.js';
import precompressor from '../js/state/handlers/url/precompressor.js';

describe('precompressor', () => {
    for (const [code, expected] of [
        ['public class C() {}', '@3 @2 C() {}']
    ]) {
        test(`compresses C# code ${code} to ${expected}`, () => {
            const compressed = precompressor.compress(code, languages.csharp);
            expect(compressed).toBe(expected);
        });
    }
});
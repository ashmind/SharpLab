import * as getGistAsync from '../js/state/handlers/url/get-gist-async.js';

import languages from '../js/helpers/languages.js';
import targets from '../js/helpers/targets.js';
import url from '../js/state/handlers/url.js';

describe('legacy load', () => {
    test('loads language as csharp if empty', async () => {
        window.location.hash = '#/';
        const { options } = await url.loadAsync();
        expect(options.language).toBe(languages.csharp);
    });
});

describe('v2', () => {
    for (const [name, value] of [
        ['branchId', 'master'],
        ...Object.values(languages).map(l => ['language', l]),
        ...Object.values(targets).map(t => ['target', t]),
        ['release', true],
        ['release', false]
    ]) {
        test(`save/load preserves option '${name}' ('${value}')`, async () => {
            url.save('', { [name]: value });
            const { options } = await url.loadAsync();
            expect(options[name]).toBe(value);
        });
    }

    for (const code of [
        'public void M() {\r\n}',
        'a || b || c',
    ]) {
        test(`save/load preserves code '${code}'`, async () => {
            url.save(code, {});
            const { code: loaded } = await url.loadAsync();
            expect(loaded).toBe(code);
        });
    }
});

describe('gist', () => {
    test(`load returns code from gist`, async () => {
        getGistAsync.default = id => Promise.resolve({ code: 'code of ' + id });

        window.location.hash = '#gist:test';
        const { code } = await url.loadAsync();
        expect(code).toBe('code of test');
    });

    test(`load returns language from gist`, async () => {
        getGistAsync.default = id => Promise.resolve({ language: 'language of ' + id });

        window.location.hash = '#gist:test';
        const { options } = await url.loadAsync();
        expect(options.language).toBe('language of test');
    });

    for (let [key, target] of Object.entries(targets)) { // eslint-disable-line prefer-const
        key = key !== 'csharp' ? key : 'cs';
        test(`load returns target '${target}' for key '${key}'`, async () => {
            getGistAsync.default = () => Promise.resolve({});

            window.location.hash = '#gist:_/'+ key;
            const { options } = await url.loadAsync();
            expect(options.target).toBe(target);
        });
    }

    for (const [language, target] of [
        [languages.csharp, targets.csharp],
        [languages.vb, targets.vb],
        [languages.fsharp, targets.csharp],
    ]) {
        test(`load returns default target '${target}' for language '${language}'`, async () => {
            getGistAsync.default = () => Promise.resolve({ language });

            window.location.hash = '#gist:_';
            const { options } = await url.loadAsync();
            expect(options.target).toBe(target);
        });
    }

    test(`load returns branchId if specified`, async () => {
        getGistAsync.default = () => Promise.resolve({});

        window.location.hash = '#gist:_//branch';
        const { options } = await url.loadAsync();
        expect(options.branchId).toBe('branch');
    });

    test(`load returns null branchId if not specified`, async () => {
        getGistAsync.default = () => Promise.resolve({});

        window.location.hash = '#gist:_/_';
        const { options } = await url.loadAsync();
        expect(options.branchId).toBeUndefined();
    });

    for (const [suffix,release] of [['///debug',false],['',true]]) {
        test(`load returns release ${release} for url options ${suffix}`, async () => {
            getGistAsync.default = () => Promise.resolve({});

            window.location.hash = '#gist:_' + suffix;
            const { options } = await url.loadAsync();
            expect(options.release).toBe(release);
        });
    }

    for (const [key, value, expected] of [
        ['target',   targets.vb, '#gist:xyz/vb'],
        ['branchId', 'branch',   '#gist:xyz//branch'],
        ['release',  false,      '#gist:xyz///debug'],
    ]) {
        test(`save (option '${key}') preserves gist if code is the same`, async () => {
            getGistAsync.default = id => Promise.resolve({ id, code: 'test' });

            window.location.hash = '#gist:xyz';
            await url.loadAsync();
            url.save('test', { release: true, [key]: value });
            expect(window.location.hash).toBe(expected);
        });
    }

    test(`save changes format to v2 if gist code changed`, async () => {
        getGistAsync.default = id => Promise.resolve({ id, code: 'original' });

        window.location.hash = '#gist:xyz';
        await url.loadAsync();
        url.save('updated', {});
        expect(window.location.hash).toMatch(/^#v2:/);
    });

    test(`save changes format to v2 if gist language changed`, async () => {
        getGistAsync.default = id => Promise.resolve({ id, code: 'test', language: languages.csharp });

        window.location.hash = '#gist:xyz';
        await url.loadAsync();
        url.save('test', { language: languages.fsharp });
        expect(window.location.hash).toMatch(/^#v2:/);
    });
});
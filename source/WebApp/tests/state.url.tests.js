import * as gists from '../js/helpers/github/gists.js';
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
        'void Func13() {}',
    ]) {
        test(`save/load preserves code '${code}'`, async () => {
            url.save(code, { language: languages.csharp });
            const { code: loaded } = await url.loadAsync();
            expect(loaded).toBe(code);
        });
    }
});

describe('gist', () => {
    test(`load returns code from gist`, async () => {
        gists.getGistAsync = id => Promise.resolve({ code: 'code of ' + id, options: {} });

        window.location.hash = '#gist:test';
        const { code } = await url.loadAsync();
        expect(code).toBe('code of test');
    });

    test(`load returns language from gist`, async () => {
        gists.getGistAsync = id => Promise.resolve({ options: { language: 'language of ' + id } });

        window.location.hash = '#gist:test';
        const { options } = await url.loadAsync();
        expect(options.language).toBe('language of test');
    });

    for (let [key, target] of Object.entries(targets)) { // eslint-disable-line prefer-const
        key = key !== 'csharp' ? key : 'cs';
        test(`load returns target '${target}' for key '${key}'`, async () => {
            gists.getGistAsync = () => Promise.resolve({ options: {} });

            window.location.hash = '#gist:_/'+ key;
            const { options } = await url.loadAsync();
            expect(options.target).toBe(target);
        });
    }

    for (const language of Object.values(languages)) {
        test(`load returns default target '${targets.csharp}' for language '${language}'`, async () => {
            gists.getGistAsync = () => Promise.resolve({ options: { language } });

            window.location.hash = '#gist:_';
            const { options } = await url.loadAsync();
            expect(options.target).toBe(targets.csharp);
        });
    }

    test(`load returns branchId if specified`, async () => {
        gists.getGistAsync = () => Promise.resolve({ options: {} });

        window.location.hash = '#gist:_//branch';
        const { options } = await url.loadAsync();
        expect(options.branchId).toBe('branch');
    });

    test(`load returns null branchId if not specified`, async () => {
        gists.getGistAsync = () => Promise.resolve({ options: {} });

        window.location.hash = '#gist:_/_';
        const { options } = await url.loadAsync();
        expect(options.branchId).toBeUndefined();
    });

    for (const [suffix,release] of [['///debug',false],['',true]]) {
        test(`load returns release ${release} for url options ${suffix}`, async () => {
            gists.getGistAsync = () => Promise.resolve({ options: {} });

            window.location.hash = '#gist:_' + suffix;
            const { options } = await url.loadAsync();
            expect(options.release).toBe(release);
        });
    }

    for (const [key, gistValue, newValue] of [
        ['language', targets.cs, targets.vb],
        ['target',   targets.cs, targets.vb],
        ['branchId', null, 'branch'],
        ['branchId', 'branch', null],
        ['release',  false, true],
        ['release',  true, false]
    ]) {
        test(`save (option '${key}') changes format to v2 if option changed`, async () => {
            gists.getGistAsync = id => Promise.resolve({ id, code: 'test', options: { [key]: gistValue } });

            window.location.hash = '#gist:xyz';
            await url.loadAsync();
            url.save('test', { release: true, [key]: newValue });
            expect(window.location.hash).toMatch(/^#v2:/);
        });
    }

    test(`save changes format to v2 if gist code changed`, async () => {
        gists.getGistAsync = id => Promise.resolve({ id, code: 'original', options: {} });

        window.location.hash = '#gist:xyz';
        await url.loadAsync();
        url.save('updated', {});
        expect(window.location.hash).toMatch(/^#v2:/);
    });
});
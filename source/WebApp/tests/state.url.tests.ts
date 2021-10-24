import * as gists from '../ts/helpers/github/gists';
import { languages, LanguageName } from '../ts/helpers/languages';
import { TargetName, targets } from '../ts/helpers/targets';
import { loadStateFromUrlAsync, saveStateToUrl, StateLoadedFromUrl } from '../ts/state/handlers/url';
import { fromPartial, asMutable } from './helpers';

const noOptions = {} as Partial<Exclude<StateLoadedFromUrl['options'], undefined>>;

describe('legacy load', () => {
    test('loads language as csharp if empty', async () => {
        window.location.hash = '#/';
        const { options = noOptions } = await loadStateFromUrlAsync()!;
        expect(options.language).toBe(languages.csharp);
    });
});

describe('v2', () => {
    for (const [name, value] of ([
        ['branchId', 'main'],
        ...Object.values(languages).map(l => ['language', l]),
        ...Object.values(targets).map(t => ['target', t]),
        ['release', true],
        ['release', false]
    ] as const)) {
        test(`save/load preserves option '${name}' ('${value}')`, async () => {
            saveStateToUrl('', fromPartial({ [name]: value }));
            const { options } = await loadStateFromUrlAsync()!;
            expect((options as { [key: string]: string|boolean })[name]).toBe(value);
        });
    }

    for (const code of [
        'public void M() {\r\n}',
        'a || b || c',
        'void Func13() {}'
    ] as const) {
        test(`save/load preserves code '${code}'`, async () => {
            saveStateToUrl(code, fromPartial({ language: languages.csharp }));
            const { code: loaded } = await loadStateFromUrlAsync()!;
            expect(loaded).toBe(code);
        });
    }
});

describe('gist', () => {
    test(`load returns code from gist`, async () => {
        asMutable(gists).getGistAsync = id => Promise.resolve(fromPartial({ code: 'code of ' + id, options: {} }));

        setLocationPath('/u/gist-test');
        const { code } = await loadStateFromUrlAsync()!;
        expect(code).toBe('code of test');
    });

    test(`load returns language from gist`, async () => {
        asMutable(gists).getGistAsync = id => Promise.resolve(fromPartial({ options: { language: 'language of ' + id as LanguageName } }));

        setLocationPath('/u/gist-test');
        const { options = noOptions } = await loadStateFromUrlAsync()!;
        expect(options.language).toBe('language of test');
    });

    test(`load returns target from gist`, async () => {
        asMutable(gists).getGistAsync = id => Promise.resolve(fromPartial({ options: { target: 'target of ' + id as TargetName } }));

        setLocationPath('/u/gist-test');
        const { options = noOptions } = await loadStateFromUrlAsync()!;
        expect(options.target).toBe('target of test');
    });

    test.each(Object.values(languages))(`load returns default target '${targets.csharp}' for language '%s'`, async language => {
        asMutable(gists).getGistAsync = () => Promise.resolve(fromPartial({ options: { language } }));

        setLocationPath('/u/gist-test');
        const { options = noOptions } = await loadStateFromUrlAsync()!;
        expect(options.target).toBe(targets.csharp);
    });

    test(`load returns release from gist`, async () => {
        asMutable(gists).getGistAsync = () => Promise.resolve(fromPartial({ options: { release: true } }));

        setLocationPath('/u/gist-test');
        const { options = noOptions } = await loadStateFromUrlAsync()!;
        expect(options.release).toBe(true);
    });

    test(`load returns null branchId if not specified`, async () => {
        asMutable(gists).getGistAsync = () => Promise.resolve(fromPartial({ options: {} }));

        setLocationPath('/u/gist-test');
        const { options = noOptions } = await loadStateFromUrlAsync()!;
        expect(options.branchId).toBeUndefined();
    });

    for (const [key, gistValue, newValue] of [
        ['language', targets.csharp, targets.vb],
        ['target',   targets.csharp, targets.vb],
        ['branchId', null, 'branch'],
        ['branchId', 'branch', null],
        ['release',  false, true],
        ['release',  true, false]
    ] as const) {
        test(`save (option '${key}') changes to custom url data string if option changed`, async () => {
            asMutable(gists).getGistAsync = id => Promise.resolve(fromPartial({ id, code: 'test', options: { [key]: gistValue } }));

            setLocationPath('/u/gist-test');
            await loadStateFromUrlAsync();
            saveStateToUrl('test', fromPartial({ release: true, [key]: newValue }));
            expect(window.location.pathname).toMatch(/^\/u\//);
        });
    }

    test(`save changes to custom url data string if gist code changed`, async () => {
        asMutable(gists).getGistAsync = id => Promise.resolve(fromPartial({ id, code: 'original', options: {} }));

        setLocationPath('/u/gist-test');
        await loadStateFromUrlAsync();
        saveStateToUrl('updated', fromPartial({}));
        expect(window.location.pathname).toMatch(/^\/u\//);
    });
});


describe('gist (from legacy hash)', () => {
    test(`load returns code from gist`, async () => {
        asMutable(gists).getGistAsync = id => Promise.resolve(fromPartial({ code: 'code of ' + id, options: {} }));

        window.location.hash = '#gist:test';
        const { code } = await loadStateFromUrlAsync()!;
        expect(code).toBe('code of test');
    });

    test(`load returns language from gist`, async () => {
        asMutable(gists).getGistAsync = id => Promise.resolve(fromPartial({ options: { language: 'language of ' + id as LanguageName } }));

        window.location.hash = '#gist:test';
        const { options = noOptions } = await loadStateFromUrlAsync()!;
        expect(options.language).toBe('language of test');
    });

    for (let [key, target] of Object.entries(targets)) { // eslint-disable-line prefer-const
        key = key !== 'csharp' ? key : 'cs';
        test(`load returns target '${target}' for key '${key}'`, async () => {
            asMutable(gists).getGistAsync = () => Promise.resolve(fromPartial({ options: {} }));

            window.location.hash = '#gist:_/' + key;
            const { options = noOptions } = await loadStateFromUrlAsync()!;
            expect(options.target).toBe(target);
        });
    }

    test.each(Object.values(languages))(`load returns default target '${targets.csharp}' for language '%s'`, async language => {
        asMutable(gists).getGistAsync = () => Promise.resolve(fromPartial({ options: { language } }));

        window.location.hash = '#gist:_';
        const { options = noOptions } = await loadStateFromUrlAsync()!;
        expect(options.target).toBe(targets.csharp);
    });

    test(`load returns branchId if specified`, async () => {
        asMutable(gists).getGistAsync = () => Promise.resolve(fromPartial({ options: {} }));

        window.location.hash = '#gist:_//branch';
        const { options = noOptions } = await loadStateFromUrlAsync()!;
        expect(options.branchId).toBe('branch');
    });

    test(`load returns null branchId if not specified`, async () => {
        asMutable(gists).getGistAsync = () => Promise.resolve(fromPartial({ options: {} }));

        window.location.hash = '#gist:_/_';
        const { options = noOptions } = await loadStateFromUrlAsync()!;
        expect(options.branchId).toBeUndefined();
    });

    for (const [suffix, release] of [['///debug', false], ['', true]] as const) {
        test(`load returns release ${release} for url options ${suffix}`, async () => {
            asMutable(gists).getGistAsync = () => Promise.resolve(fromPartial({ options: {} }));

            window.location.hash = '#gist:_' + suffix;
            const { options = noOptions } = await loadStateFromUrlAsync()!;
            expect(options.release).toBe(release);
        });
    }

    for (const [key, gistValue, newValue] of [
        ['language', targets.csharp, targets.vb],
        ['target',   targets.csharp, targets.vb],
        ['branchId', null, 'branch'],
        ['branchId', 'branch', null],
        ['release',  false, true],
        ['release',  true, false]
    ] as const) {
        test(`save (option '${key}') changes to custom url data string if option changed`, async () => {
            asMutable(gists).getGistAsync = id => Promise.resolve(fromPartial({ id, code: 'test', options: { [key]: gistValue } }));

            window.location.hash = '#gist:xyz';
            await loadStateFromUrlAsync();
            saveStateToUrl('test', fromPartial({ release: true, [key]: newValue }));
            expect(window.location.pathname).toMatch(/^\/u\//);
        });
    }

    test(`save changes to custom url data string if gist code changed`, async () => {
        asMutable(gists).getGistAsync = id => Promise.resolve(fromPartial({ id, code: 'original', options: {} }));

        window.location.hash = '#gist:xyz';
        await loadStateFromUrlAsync();
        saveStateToUrl('updated', fromPartial({}));
        expect(window.location.pathname).toMatch(/^\/u\//);
    });
});

afterEach(() => {
    setLocationPath('/');
    window.location.hash = '';
});

function setLocationPath(path: string) {
    history.replaceState(null, '', path);
}
import { createGistAsync, getGistAsync } from '../js/helpers/github/gists.js';
import * as auth from '../js/helpers/github/auth.js';

// @ts-ignore
auth.token = '_';

describe('getGistAsync', () => {
    test('roundtrips options from createGistAsync', async () => {
        const options = {
            branchId:   'test-branch',
            language:   'C#',
            target:     'test-target',
            release:    true
        };
        let files;
        // @ts-ignore
        window.fetch = async (_, { body } = {}) => {
            // @ts-ignore
            files = JSON.parse(body).files;
            return { ok: true, json: async () => ({}) };
        };
        // @ts-ignore
        await createGistAsync({ name: '_', result: {}, options });
        // @ts-ignore
        window.fetch = async () => ({ ok: true, json: async () => ({ files }) });

        const result = await getGistAsync();

        expect(result.options).toEqual(options);
    });
});

afterAll(() => {
    window.fetch = null;
});
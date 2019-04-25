import { createGistAsync, getGistAsync } from '../js/helpers/github/gists.js';
import * as auth from '../js/helpers/github/auth.js';

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
        global.fetch = async (_, { body } = {}) => {
            files = JSON.parse(body).files;
            return { ok: true, json: async () => ({}) };
        };
        await createGistAsync({ name: '_', result: {}, options });
        global.fetch = async () => ({ ok: true, json: async () => ({ files }) });

        const result = await getGistAsync();

        expect(result.options).toEqual(options);
    });
});

afterAll(() => {
    global.fetch = null;
});
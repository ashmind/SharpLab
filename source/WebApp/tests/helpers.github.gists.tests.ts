import { createGistAsync, getGistAsync } from '../ts/helpers/github/gists';
import * as auth from '../ts/helpers/github/auth';
import { fromPartial, asMutable } from './helpers';

asMutable(auth).token = '_';

describe('getGistAsync', () => {
    test('roundtrips options from createGistAsync', async () => {
        const options = {
            branchId: 'test-branch',
            language: 'C#',
            target:   'IL',
            release:  true
        } as const;
        let files: [{ [key: string]: { content: string }|undefined }];
        window.fetch = async (_, { body }: { body?: unknown } = {}) => {
            ({ files } = JSON.parse(body as string));
            return fromPartial({ ok: true, json: async () => ({}) });
        };
        await createGistAsync(fromPartial({ name: '_', result: {}, options }));
        window.fetch = async () => fromPartial({ ok: true, json: async () => ({ files }) });

        const result = await getGistAsync('_');

        expect(result.options).toEqual(options);
    });
});

afterAll(() => {
    (window as { fetch: typeof window.fetch|null }).fetch = null;
});
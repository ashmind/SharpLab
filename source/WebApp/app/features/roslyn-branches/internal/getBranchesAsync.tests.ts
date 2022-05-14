import { fromPartial } from '../../../helpers/testing/fromPartial';

describe('get-branches-async', () => {
    test.each([
        // main domain (not overridable)
        ['sharplab.io', '', 'https://slbs.azureedge.net/public/branches.json'],

        // edge
        ['edge.sharplab.io', '', 'https://slbs.azureedge.net/public/branches-edge.json'],
        ['edge.sharplab.io', 'main', 'https://slbs.azureedge.net/public/branches.json'],
        ['edge.sharplab.io', 'edge', 'https://slbs.azureedge.net/public/branches-edge.json'],

        // local
        ['sharplab.local', '', '!branches.json'],
        ['sharplab.local', 'main', 'https://slbs.azureedge.net/public/branches.json'],
        ['sharplab.local', 'edge', 'https://slbs.azureedge.net/public/branches-edge.json']
    ] as const)("fetches expected url for domain '%s' and query string '%s'", async (host, query, expected) => {
        window.fetch = jest.fn();
        delete (window as { location?: Window['location'] }).location;

        window.location = fromPartial({
            host,
            search: query ? `?branches=${query}` : ''
        });

        jest.resetModules();
        // eslint-disable-next-line @typescript-eslint/no-unsafe-call
        const { getBranchesAsync } = (await import('./getBranchesAsync'));

        await getBranchesAsync();

        expect(fetch).toHaveBeenCalledTimes(1);
        expect(fetch).toHaveBeenCalledWith(expected);
    });
});

(() => {
    let location: typeof window.location;
    beforeEach(() => {
        location = window.location;
    });

    afterEach(() => {
        delete (window as { fetch?: Window['fetch'] }).fetch;
        window.location = location;
    });
})();
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
    ])("fetches expected url for domain '%s' and query string '%s'", async (host, query, expected) => {
        window.fetch = jest.fn();
        delete window.location;
        // @ts-ignore
        window.location = {
            host,
            search: query ? `?branches=${query}` : ''
        };

        jest.resetModules();
        const getBranchesAsync = (await import('../js/server/get-branches-async.js')).default;

        await getBranchesAsync();

        expect(fetch).toHaveBeenCalledTimes(1);
        expect(fetch).toHaveBeenCalledWith(expected);
    });
});

(() => {
    let location;
    beforeEach(() => {
        location = window.location;
    });

    afterEach(() => {
        delete window.fetch;
        window.location = location;
    });
})();
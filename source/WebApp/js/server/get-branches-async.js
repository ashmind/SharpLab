const branchesUrl = (() => {
    const mainUrl = 'https://slbs.azureedge.net/public/branches.json';
    const edgeUrl = 'https://slbs.azureedge.net/public/branches-edge.json';

    const host = window.location.host;
    const override = (window.location.search.match(/[?&]branches=([^?&]+)/) || [])[1];

    switch (host) {
        case 'sharplab.io':
            if (override)
                throw new Error('Cannot override branch source on the main site (remove ?branches=).');
            return mainUrl;
        case 'edge.sharplab.io':
            return override === 'main' ? mainUrl : edgeUrl;
        default:
            return { main: mainUrl, edge: edgeUrl }[override] || '!branches.json';
    }
})();

export default async function getBranchesAsync() {
    try {
        const branches = await (await fetch(branchesUrl)).json();
        for (const branch of branches) {
            if (!branch.commits)
                continue;
            for (const commit of branch.commits) {
                commit.date = new Date(commit.date);
            }
        }

        return branches;
    }
    catch(e) {
        return [];
    }
}
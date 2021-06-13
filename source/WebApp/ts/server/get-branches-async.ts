import type { PartiallyMutable } from '../helpers/partially-mutable';
import type { Branch, BranchCommit } from '../types/branch';
import asLookup from '../helpers/as-lookup';

const branchesUrl = (() => {
    const mainUrl = 'https://slbs.azureedge.net/public/branches.json';
    const edgeUrl = 'https://slbs.azureedge.net/public/branches-edge.json';

    const host = window.location.host;
    const override = (window.location.search.match(/[?&]branches=([^?&]+)/) ?? [])[1];

    switch (host) {
        case 'sharplab.io':
            if (override)
                throw new Error('Cannot override branch source on the main site (remove ?branches=).');
            return mainUrl;
        case 'edge.sharplab.io':
            return override === 'main' ? mainUrl : edgeUrl;
        default:
            return asLookup({ main: mainUrl, edge: edgeUrl })[override] ?? '!branches.json';
    }
})();

export default async function getBranchesAsync(): Promise<ReadonlyArray<Branch>> {
    try {
        const branches = await (await fetch(branchesUrl)).json() as ReadonlyArray<Omit<Branch, 'commits'> & {
            commits?: ReadonlyArray<Omit<BranchCommit, 'date'> & { date: string }>;
        }>;
        for (const branch of branches) {
            if (!branch.commits)
                continue;
            for (const commit of branch.commits) {
                (commit as unknown as PartiallyMutable<BranchCommit, 'date'>).date = new Date(commit.date);
            }
        }
        return branches as ReadonlyArray<Branch>;
    }
    catch (e) {
        return [];
    }
}
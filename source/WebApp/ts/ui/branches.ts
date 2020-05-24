import dateFormat from 'dateformat';
import type { PartiallyMutable } from '../helpers/partially-mutable';
import type { Branch } from '../types/branch';
import getBranchesAsync from '../server/get-branches-async';

function getBranchDisplayName(branch: Branch) {
    const feature = branch.feature;
    let displayName = feature
        ? `${feature.language}: ${feature.name}`
        : branch.name;
    if (branch.commits)
        displayName += ` (${dateFormat(branch.commits[0].date, 'd mmm yyyy')})`;

    return displayName;
}

export const branchesPromise = (async () => {
    const branches = await getBranchesAsync();
    for (const branch of branches) {
        (branch as PartiallyMutable<Branch, 'displayName'>).displayName = getBranchDisplayName(branch);
    }
    return branches;
})();

export async function resolveBranch(branchId: string) {
    const branches = await branchesPromise;
    return branches.find(b => b.id === branchId) ?? null;
}
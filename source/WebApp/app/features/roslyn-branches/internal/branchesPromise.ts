import dateFormat from 'dateformat';
import type { PartiallyMutable } from '../../../shared/helpers/partiallyMutable';
import type { Branch } from '../types';
import { getBranchesAsync } from './getBranchesAsync';

const getBranchDisplayName = (branch: Branch) => {
    const feature = branch.feature;
    let displayName = feature
        ? `${feature.language}: ${feature.name}`
        : branch.name;
    if (branch.commits?.[0])
        displayName += ` (${dateFormat(branch.commits[0].date, 'd mmm yyyy')})`;

    return displayName;
};

export const branchesPromise = (async () => {
    const branches = await getBranchesAsync();
    for (const branch of branches) {
        (branch as PartiallyMutable<Branch, 'displayName'>).displayName = getBranchDisplayName(branch);
    }
    return branches;
})();
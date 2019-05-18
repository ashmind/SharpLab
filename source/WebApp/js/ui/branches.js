import dateFormat from 'dateformat';

function getBranchDisplayName(branch) {
    const feature = branch.feature;
    let displayName = feature
        ? `${feature.language}: ${feature.name}`
        : branch.name;
    if (branch.commits)
        displayName += ` (${dateFormat(branch.commits[0].date, 'd mmm yyyy')})`;

    return displayName;
}

function groupAndSortBranches(branches) {
    const result = {
        groups: [],
        ungrouped: []
    };

    const groups = {};
    for (const branch of branches) {
        if (!branch.group) {
            result.ungrouped.push(branch);
            continue;
        }

        let group = groups[branch.group];
        if (!group) {
            group = { name: branch.group, kind: branch.kind, branches: [] };
            groups[branch.group] = group;
            result.groups.push(group);
        }
        group.branches.push(branch);
    }

    result.groups.sort(groupSortOrder);

    for (const group of result.groups) {
        if (group.name === 'Platforms')
            continue; // do not sort Platforms

        group.branches.sort(branchSortOrder);
    }

    return result;
}

function groupSortOrder(a, b) {
    // 'Platform' always goes first
    if (a.name === 'Platforms') return -1;
    if (b.name === 'Platforms') return +1;

    // otherwise by name
    if (a.name > b.name) return +1;
    if (a.name < b.name) return -1;
    return 0;
}

function branchSortOrder(a, b) {
    // master always goes first
    if (a.name === 'master') return -1;
    if (b.name === 'master') return +1;

    // if this has a language, sort by language first, with newer lang versions on top
    if (a.feature) {
        if (!b.feature || b.feature.language < a.feature.language) return -1;
        if (b.feature.language > a.feature.language) return 1;
    }
    else if (b.feature) {
        return 1;
    }

    // otherwise by displayName
    if (a.displayName > b.displayName) return +1;
    if (a.displayName < b.displayName) return -1;
    return 0;
}

export {
    getBranchDisplayName,
    groupAndSortBranches
};
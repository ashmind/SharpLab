export default async function getBranchesAsync() {
    try {
        const branches = await (await fetch('!branches.json')).json();
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
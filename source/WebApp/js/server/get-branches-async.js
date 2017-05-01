export default async function getBranchesAsync() {
    try {
        const branches = await fetch('!branches.json');
        for (let branch of branches) {
            for (let commit of branch.commits) {
                commit.date = new Date(commit.date);
            }
        }

        return branches;
    }
    catch(e) {
        return [];
    }
}
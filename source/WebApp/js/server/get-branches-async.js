import $ from 'jquery';

export default async function getBranchesAsync() {
    try {
        const branches = await $.get('!branches.json');
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
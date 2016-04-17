import $ from 'jquery';

export default async function getBranchesAsync() {
    try {
        return await $.get('!branches.json');
    }
    catch(e) {
        return [];
    }
}